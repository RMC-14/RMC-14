using System.Linq;
using Content.Client._RMC14.UserInterface;
using Content.Client.Eui;
using Content.Shared._RMC14.Admin.PayloadDeployment;
using Content.Shared._RMC14.PayloadDeployment;
using Content.Shared.Eui;
using Content.Shared.Prototypes;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Admin.PayloadDeployment;

[UsedImplicitly]
public sealed class RMCPayloadDeploymentEui : BaseEui
{
    private const int MaxNearbyRadius = 50;
    private const int MaxManifestNameLength = 32;
    private const int MaxPrototypeResults = 250;

    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    private readonly List<RMCPayloadDeploymentManifest> _manifests = [];
    private readonly List<RMCPayloadDeploymentMapEntry> _maps = [];
    private readonly List<EntityPrototype> _prototypeCatalog = [];

    private SharedTransformSystem _transform = default!;
    private RMCPayloadDeploymentDraftSystem _draft = default!;
    private RMCPayloadDeploymentControl _control = default!;
    private RMCPayloadDeploymentWindow? _hostWindow;
    private WindowRoot? _popOutRoot;
    private IClydeWindow? _popOutWindow;
    private List<RMCPayloadDeploymentEntityEntry> _nearby = [];
    private List<RMCPayloadDeploymentEntityEntry> _playerControlled = [];
    private ConfirmationWindow? _confirmation;
    private RMCPayloadDeliveryType _deliveryType;
    private int _activeManifest;
    private bool _initializeTarget = true;
    private bool _loadingManifest;
    private List<PendingManifest>? _pendingManifests;

    private RMCPayloadDeploymentManifest ActiveManifest => _manifests[_activeManifest];

    public override void Opened()
    {
        _transform = _entities.System<SharedTransformSystem>();
        _draft = _entities.System<RMCPayloadDeploymentDraftSystem>();
        _hostWindow = new RMCPayloadDeploymentWindow();
        _control = _hostWindow.Deployment;
        _hostWindow.OnClose += SendClosedMessage;

        if (_draft.TryRestore(out var deliveryType, out var activeManifest, out var manifests))
        {
            _deliveryType = deliveryType;
            _activeManifest = activeManifest;
            _manifests.AddRange(manifests);
            _initializeTarget = false;
        }

        if (_manifests.Count == 0)
            _manifests.Add(new RMCPayloadDeploymentManifest());

        for (var i = 0; i < _manifests.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(_manifests[i].Name))
            {
                _manifests[i].Name = Loc.GetString("rmc-payload-deployment-manifest-number", ("number", i + 1));
            }
        }

        _control.DropType.AddItem(Loc.GetString("rmc-payload-deployment-orbital"), (int) RMCPayloadDeliveryType.Orbital);
        _control.DropType.AddItem(Loc.GetString("rmc-payload-deployment-paradrop"), (int) RMCPayloadDeliveryType.ParaDrop);
        _control.DropType.SelectId((int) _deliveryType);

        _control.NearbyRadius.IsValid = value => value is >= 1 and <= MaxNearbyRadius;
        _control.PrototypeQuantity.IsValid = value => value is >= 1 and <= RMCPayloadDeploymentLimits.MaxPayload;
        _control.LandingRadius.IsValid = value =>
            value is >= 0 and <= RMCPayloadDeploymentLimits.MaxLandingRadius &&
            MathF.Abs(value * 2 - MathF.Round(value * 2)) < 0.001f;
        _control.PodCount.IsValid = value => value >= 1 &&
            value <= Math.Max(1, Math.Min(RMCPayloadDeploymentLimits.MaxOrbitalDrops, ActiveManifest.PayloadCount()));
        _control.LaunchInterval.IsValid = ValidDuration;
        _control.ArrivalDelay.IsValid = ValidDuration;
        _control.ArrivalInterval.IsValid = ValidDuration;
        _control.ArrivalIntervalVariation.IsValid = ValidDuration;
        _control.DropDuration.IsValid = ValidDuration;
        _control.OpenDelay.IsValid = ValidDuration;
        _control.ManifestName.IsValid = value => value.Length <= MaxManifestNameLength;

        _control.RefreshNearby.OnPressed += _ => Refresh(_control.NearbyRadius.Value);
        _control.SelectAllNearby.OnPressed += _ => _control.NearbyEntities.ToggleAll();
        _control.NearbyEntities.SelectionChanged += UpdateNearbySelectionButton;
        _control.AddNearby.OnPressed += _ => AddSelectedEntities(_control.NearbyEntities, true);
        _control.AddPlayer.OnPressed += _ => AddSelectedEntities(_control.PlayerEntities, false);
        _control.AddPrototype.OnPressed += _ => AddSelectedPrototype();
        _control.RemoveManifestEntry.OnPressed += _ => RemoveSelectedManifestEntry();
        _control.ClearManifest.OnPressed += _ => ClearManifestEntries();
        _control.UseCurrentPosition.OnPressed += _ => SetCurrentPosition();
        _control.AddManifest.OnPressed += _ => AddManifest();
        _control.RemoveManifest.OnPressed += _ => RemoveManifest();
        _hostWindow.PopOut.OnPressed += _ => PopOut();
        _control.LaunchSelected.OnPressed += _ => ConfirmLaunch(true);
        _control.LaunchAll.OnPressed += _ => ConfirmLaunch(false);

        _control.NearbySearch.OnTextChanged += _ => RebuildNearbyList();
        _control.PlayerSearch.OnTextChanged += _ => RebuildPlayerList();
        _control.PrototypeSearch.OnTextChanged += _ => RebuildPrototypeList();
        _control.LandingRadius.OnValueChanged += _ => UpdateLandingTileCount();
        _control.UseDropPods.OnToggled += args =>
        {
            if (!_loadingManifest)
                ActiveManifest.UseDropPods = args.Pressed;

            UpdateModeControls();
        };
        _control.ManifestName.OnTextChanged += args =>
        {
            if (_loadingManifest)
                return;

            ActiveManifest.Name = args.Text;
            UpdateManifestOptions();
            RebuildNearbyList();
            RebuildPlayerList();
        };
        _control.DropType.OnItemSelected += OnDropTypeSelected;
        _control.ManifestOptions.OnItemSelected += OnManifestSelected;
        _control.MapOptions.OnItemSelected += args =>
        {
            _control.MapOptions.SelectId(args.Id);
            if (!_loadingManifest && args.Id >= 0 && args.Id < _maps.Count)
            {
                ActiveManifest.Map = _maps[args.Id].MapId;
                UpdateManifestOptions();
            }

            UpdateCoordinateMode();
        };
        _control.RawCoordinates.OnToggled += args =>
        {
            if (_loadingManifest ||
                _control.MapOptions.SelectedId < 0 ||
                _control.MapOptions.SelectedId >= _maps.Count ||
                !_maps[_control.MapOptions.SelectedId].HasPlanetCoordinates)
            {
                return;
            }

            var map = _maps[_control.MapOptions.SelectedId];
            var coordinates = new Vector2i(_control.MapX.Value, _control.MapY.Value);
            coordinates += args.Pressed
                ? -map.CoordinateOffset
                : map.CoordinateOffset;

            _control.MapX.Value = coordinates.X;
            _control.MapY.Value = coordinates.Y;
            ActiveManifest.Coordinates = coordinates;
            ActiveManifest.RawCoordinates = args.Pressed;
        };

        BuildPrototypeCatalog();
        UpdateModeControls();
        LoadActiveManifest();
        _hostWindow.OpenCentered();
    }

    public override void Closed()
    {
        SaveActiveManifest();
        _draft.Save(_deliveryType, _activeManifest, _manifests);
        _confirmation?.Close();
        _confirmation = null;
        if (_hostWindow != null)
            _hostWindow.OnClose -= SendClosedMessage;

        if (_popOutWindow != null)
            _popOutWindow.RequestClosed -= OnPopOutClosed;

        _control.Orphan();
        _hostWindow?.Close();
        _popOutWindow?.Dispose();
    }

    private void OnPopOutClosed(WindowRequestClosedEventArgs args)
    {
        SendClosedMessage();
    }

    private void PopOut()
    {
        if (_hostWindow == null)
            return;

        _confirmation?.Close();
        _confirmation = null;

        var monitor = _clyde.EnumerateMonitors().First();
        _popOutWindow = _clyde.CreateWindow(new WindowCreateParameters
        {
            Maximized = false,
            Title = Loc.GetString("rmc-payload-deployment-title"),
            Monitor = monitor,
            Width = 1400,
            Height = 1000,
        });

        _control.Orphan();
        _hostWindow.OnClose -= SendClosedMessage;
        _hostWindow.Close();
        _hostWindow = null;

        _popOutWindow.RequestClosed += OnPopOutClosed;
        _popOutWindow.DisposeOnClose = true;

        _popOutRoot = _uiManager.CreateWindowRoot(_popOutWindow);
        _popOutRoot.AddChild(_control);
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not RMCPayloadDeploymentEuiState deploymentState)
            return;

        _nearby = deploymentState.Nearby;
        _playerControlled = deploymentState.PlayerControlled;
        _control.NearbyRadius.Value = deploymentState.NearbyRadius;
        UpdateMaps(deploymentState.Maps);
        RebuildNearbyList();
        RebuildPlayerList();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        if (msg is RMCPayloadDeploymentInvalidEntitiesMsg invalid)
        {
            var removed = false;
            foreach (var manifest in _manifests)
            {
                foreach (var entity in invalid.Entities)
                {
                    removed |= manifest.Entities.Remove(entity);
                }
            }

            if (!removed)
                return;

            UpdateManifest();
            RebuildNearbyList();
            RebuildPlayerList();

            return;
        }

        if (msg is not RMCPayloadDeploymentResultMsg result)
            return;

        var pending = _pendingManifests ?? [];
        _pendingManifests = null;
        SetInputBlocked(false);
        SetLaunchButtonsDisabled(false);
        if (result.Failure == RMCPayloadDeploymentFailure.None)
        {
            foreach (var pendingManifest in pending)
            {
                if (!_manifests.Contains(pendingManifest.Original))
                    continue;

                foreach (var entity in pendingManifest.Snapshot.Entities.Keys)
                    pendingManifest.Original.Entities.Remove(entity);
            }

            _control.Result.Text = Loc.GetString("rmc-payload-deployment-result-success", ("manifests", pending.Count));
            UpdateManifest();
            RebuildNearbyList();
            RebuildPlayerList();
            _draft.Save(_deliveryType, _activeManifest, _manifests);
            return;
        }

        PendingManifest? failedManifest = result.FailedManifest >= 0 && result.FailedManifest < pending.Count
            ? pending[result.FailedManifest]
            : null;
        var manifestLabel = failedManifest != null
            ? failedManifest.Label
            : Loc.GetString("rmc-payload-deployment-unknown-manifest");
        if (failedManifest != null)
        {
            var failedIndex = _manifests.IndexOf(failedManifest.Original);
            if (failedIndex >= 0)
            {
                SaveActiveManifest();
                _activeManifest = failedIndex;
                LoadActiveManifest();
            }
        }

        _control.Result.Text = result.Failure switch
        {
            RMCPayloadDeploymentFailure.InvalidPayload => Loc.GetString("rmc-payload-deployment-result-invalid-payload", ("manifest", manifestLabel)),
            RMCPayloadDeploymentFailure.InvalidPrototype => Loc.GetString("rmc-payload-deployment-result-invalid-prototype", ("manifest", manifestLabel)),
            RMCPayloadDeploymentFailure.InvalidSettings => Loc.GetString("rmc-payload-deployment-result-invalid-settings", ("manifest", manifestLabel)),
            RMCPayloadDeploymentFailure.InvalidTarget => Loc.GetString("rmc-payload-deployment-result-invalid-target", ("manifest", manifestLabel)),
            RMCPayloadDeploymentFailure.InsufficientLandingTiles => Loc.GetString(
                "rmc-payload-deployment-result-insufficient-tiles",
                ("manifest", manifestLabel),
                ("requested", result.RequestedLandings),
                ("assigned", result.AssignedLandings)),
            RMCPayloadDeploymentFailure.PodPreparationFailed => Loc.GetString(
                "rmc-payload-deployment-result-preparation-failed", ("manifest", manifestLabel)),
            _ => Loc.GetString("rmc-payload-deployment-result-failed"),
        };
    }

    private void OnDropTypeSelected(OptionButton.ItemSelectedEventArgs args)
    {
        if (_loadingManifest)
            return;

        SaveActiveManifest();
        _deliveryType = (RMCPayloadDeliveryType) args.Id;
        _control.DropType.SelectId(args.Id);

        BuildPrototypeCatalog();
        UpdateModeControls();
        LoadActiveManifest();
    }

    private void OnManifestSelected(OptionButton.ItemSelectedEventArgs args)
    {
        if (_loadingManifest || args.Id < 0 || args.Id >= _manifests.Count)
            return;

        SaveActiveManifest();
        _activeManifest = args.Id;
        _control.ManifestOptions.SelectId(args.Id);
        LoadActiveManifest();
        RebuildNearbyList();
        RebuildPlayerList();
    }

    private void AddManifest()
    {
        if (_manifests.Count >= RMCPayloadDeploymentLimits.MaxBatchRequests)
            return;

        SaveActiveManifest();
        var manifest = ActiveManifest.CopySettings();
        manifest.Name = Loc.GetString("rmc-payload-deployment-manifest-number", ("number", _manifests.Count + 1));
        _manifests.Add(manifest);
        _activeManifest = _manifests.Count - 1;

        LoadActiveManifest();
        RebuildNearbyList();
        RebuildPlayerList();
    }

    private void RemoveManifest()
    {
        if (_manifests.Count <= 1)
            return;

        _manifests.RemoveAt(_activeManifest);
        for (var i = _activeManifest; i < _manifests.Count; i++)
        {
            var previousDefault = Loc.GetString("rmc-payload-deployment-manifest-number", ("number", i + 2));
            if (_manifests[i].Name == previousDefault)
                _manifests[i].Name = Loc.GetString("rmc-payload-deployment-manifest-number", ("number", i + 1));
        }

        _activeManifest = Math.Min(_activeManifest, _manifests.Count - 1);
        LoadActiveManifest();
        RebuildNearbyList();
        RebuildPlayerList();
    }

    private void UpdateManifestOptions()
    {
        _loadingManifest = true;
        _control.ManifestOptions.Clear();
        for (var i = 0; i < _manifests.Count; i++)
        {
            _control.ManifestOptions.AddItem(GetManifestLabel(i, true), i);
        }

        _control.ManifestOptions.SelectId(_activeManifest);
        _control.RemoveManifest.Disabled = _manifests.Count <= 1;
        _control.AddManifest.Disabled = _manifests.Count >= RMCPayloadDeploymentLimits.MaxBatchRequests;
        _loadingManifest = false;
    }

    private string GetManifestLabel(int index, bool includePayload, bool includeMap = true)
    {
        if (index < 0 || index >= _manifests.Count)
            return Loc.GetString("rmc-payload-deployment-unknown-manifest");

        var manifest = _manifests[index];
        var label = string.IsNullOrWhiteSpace(manifest.Name)
            ? Loc.GetString("rmc-payload-deployment-manifest-number", ("number", index + 1))
            : manifest.Name.Trim();

        if (includeMap && manifest.Map is { } map)
            label += $" — {FormatMap(map)}";

        if (includePayload)
            label += $" ({manifest.PayloadCount()})";

        return label;
    }

    private string FormatMap(MapId mapId)
    {
        var id = Loc.GetString("rmc-payload-deployment-map-id", ("id", (int) mapId));
        var map = _maps.FirstOrDefault(entry => entry.MapId == mapId);
        return map == default || map.Name.Equals(id, StringComparison.OrdinalIgnoreCase)
            ? id
            : $"{id} ({map.Name})";
    }

    private void SaveActiveManifest()
    {
        if (_loadingManifest || _manifests.Count == 0)
            return;

        var manifest = ActiveManifest;
        manifest.Name = _control.ManifestName.Text;
        if (_control.MapOptions.SelectedId >= 0 && _control.MapOptions.SelectedId < _maps.Count)
            manifest.Map = _maps[_control.MapOptions.SelectedId].MapId;

        manifest.Coordinates = new Vector2i(_control.MapX.Value, _control.MapY.Value);
        manifest.LandingRadius = _control.LandingRadius.Value;
        manifest.UseDropPods = _control.UseDropPods.Pressed;
        manifest.PodCount = _control.PodCount.Value;
        manifest.ArrivalDelay = _control.ArrivalDelay.Value;
        manifest.DropDuration = _control.DropDuration.Value;
        manifest.OpenDelay = _control.OpenDelay.Value;
        manifest.LaunchInterval = _control.LaunchInterval.Value;
        manifest.ArrivalInterval = _control.ArrivalInterval.Value;
        manifest.ArrivalIntervalVariation = _control.ArrivalIntervalVariation.Value;
        manifest.UseParachute = _control.UseParachute.Pressed;
        manifest.ShowLandingWarning = _control.ShowLandingWarning.Pressed;
        manifest.RawCoordinates = _control.RawCoordinates.Pressed;
        manifest.IgnoreParadropRestrictions = _control.IgnoreParadropRestrictions.Pressed;
    }

    private void LoadActiveManifest()
    {
        if (_manifests.Count == 0)
            return;

        _loadingManifest = true;
        var manifest = ActiveManifest;
        _control.ManifestName.Text = manifest.Name;
        var mapIndex = manifest.Map is { } map
            ? _maps.FindIndex(entry => entry.MapId == map)
            : -1;
        if (mapIndex < 0 && _maps.Count > 0)
        {
            mapIndex = 0;
            manifest.Map = _maps[0].MapId;
        }

        if (mapIndex >= 0)
            _control.MapOptions.Select(mapIndex);

        _control.MapX.Value = manifest.Coordinates.X;
        _control.MapY.Value = manifest.Coordinates.Y;
        _control.LandingRadius.Value = manifest.LandingRadius;
        _control.UseDropPods.Pressed = manifest.UseDropPods;
        _control.PodCount.Value = Math.Max(1, Math.Min(manifest.PodCount, Math.Max(1, manifest.PayloadCount())));
        _control.ArrivalDelay.Value = manifest.ArrivalDelay;
        _control.DropDuration.Value = manifest.DropDuration;
        _control.OpenDelay.Value = manifest.OpenDelay;
        _control.LaunchInterval.Value = manifest.LaunchInterval;
        _control.ArrivalInterval.Value = manifest.ArrivalInterval;
        _control.ArrivalIntervalVariation.Value = manifest.ArrivalIntervalVariation;
        _control.UseParachute.Pressed = manifest.UseParachute;
        _control.ShowLandingWarning.Pressed = manifest.ShowLandingWarning;
        _control.IgnoreParadropRestrictions.Pressed = manifest.IgnoreParadropRestrictions;
        UpdateCoordinateMode();
        UpdateLandingTileCount();
        _loadingManifest = false;
        UpdateModeControls();
        UpdateManifest();
    }

    private void UpdateModeControls()
    {
        var orbital = _deliveryType == RMCPayloadDeliveryType.Orbital;
        _control.UseDropPods.Visible = orbital;
        _control.PodCountRow.Visible = orbital && _control.UseDropPods.Pressed;
        _control.OpenDelayRow.Visible = orbital && _control.UseDropPods.Pressed;
        _control.UseParachute.Visible = orbital;
        _control.ShowLandingWarning.Visible = orbital;
    }

    private void BuildPrototypeCatalog()
    {
        _prototypeCatalog.Clear();
        var categoryFilter = _configuration.GetCVar(CVars.EntitiesCategoryFilter);
        _prototypes.TryIndex<EntityCategoryPrototype>(categoryFilter, out var filter);

        foreach (var prototype in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (prototype.Abstract ||
                prototype.HideSpawnMenu ||
                prototype.HasComponent<OccluderComponent>(_entities.ComponentFactory) ||
                filter != null && !prototype.Categories.Contains(filter))
            {
                continue;
            }

            _prototypeCatalog.Add(prototype);
        }

        _prototypeCatalog.Sort((left, right) =>
        {
            var name = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
            return name != 0 ? name : string.Compare(left.ID, right.ID, StringComparison.OrdinalIgnoreCase);
        });
        RebuildPrototypeList();
    }

    private void RebuildPrototypeList()
    {
        var search = _control.PrototypeSearch.Text.Trim();
        var entries = new List<RMCPayloadDeploymentListEntry>();
        var shown = 0;
        foreach (var prototype in _prototypeCatalog)
        {
            if (!MatchesPrototype(prototype, search))
                continue;

            var prototypeName = string.IsNullOrWhiteSpace(prototype.Name) ? prototype.ID : prototype.Name;
            if (!string.IsNullOrWhiteSpace(prototype.EditorSuffix))
                prototypeName += $" [{prototype.EditorSuffix}]";

            var prototypeId = new EntProtoId(prototype.ID);
            entries.Add(new RMCPayloadDeploymentListEntry(
                prototypeName,
                prototypeId,
                prototype.Description,
                Prototype: prototypeId));

            if (++shown >= MaxPrototypeResults)
                break;
        }

        _control.Prototypes.SetItems(entries);
    }

    private static bool MatchesPrototype(EntityPrototype prototype, string search)
    {
        return string.IsNullOrEmpty(search) ||
               prototype.ID.Contains(search, StringComparison.OrdinalIgnoreCase) ||
               prototype.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
               prototype.EditorSuffix?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;
    }

    private void RebuildNearbyList()
    {
        PopulateEntityList(_control.NearbyEntities, _nearby, _control.NearbySearch.Text, false);
    }

    private void UpdateNearbySelectionButton()
    {
        _control.SelectAllNearby.Text = _control.NearbyEntities.AllSelected
            ? Loc.GetString("rmc-payload-deployment-deselect-all")
            : Loc.GetString("rmc-payload-deployment-select-all");
    }

    private void RebuildPlayerList()
    {
        PopulateEntityList(_control.PlayerEntities, _playerControlled, _control.PlayerSearch.Text, true);
    }

    private void PopulateEntityList(
        RMCPayloadDeploymentList list,
        IEnumerable<RMCPayloadDeploymentEntityEntry> entries,
        string search,
        bool showMap)
    {
        var items = new List<RMCPayloadDeploymentListEntry>();
        foreach (var entry in entries)
        {
            var assignedManifest = _manifests.FindIndex(manifest => manifest.Entities.ContainsKey(entry.Entity));
            if (assignedManifest == _activeManifest)
                continue;

            var map = FormatMap(entry.Map);
            var assignment = assignedManifest >= 0
                ? GetManifestLabel(assignedManifest, false, false)
                : string.Empty;
            var filtering = $"{entry.Name} {entry.Prototype} {entry.Entity} {entry.Role} {assignment}";
            if (showMap)
                filtering += $" {map}";

            if (!string.IsNullOrWhiteSpace(search) &&
                !filtering.Contains(search, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = assignedManifest >= 0
                ? $"[{assignment}] {entry.Name} [{entry.Entity}]"
                : $"{entry.Name} [{entry.Entity}]";
            if (!string.IsNullOrWhiteSpace(entry.Role))
                text += $" — {entry.Role}";

            if (showMap)
                text += $" — {map}";

            items.Add(new RMCPayloadDeploymentListEntry(
                text,
                entry,
                entry.Prototype,
                Entity: entry.Entity));
        }

        list.SetItems(items);
    }

    private void AddSelectedEntities(RMCPayloadDeploymentList list, bool confirmCrossMap)
    {
        var selectedEntries = list.GetSelected()
            .Select(item => item.Metadata)
            .OfType<RMCPayloadDeploymentEntityEntry>()
            .ToList();
        if (selectedEntries.Count == 0)
            return;

        if (confirmCrossMap &&
            _players.LocalEntity is { } player &&
            _entities.TryGetComponent(player, out TransformComponent? transform) &&
            selectedEntries.Any(entry => entry.Map != transform.MapID))
        {
            _confirmation?.Close();
            _confirmation = new ConfirmationWindow();
            _confirmation.Setup(
                Loc.GetString("rmc-payload-deployment-cross-map-title"),
                Loc.GetString("rmc-payload-deployment-cross-map-text"),
                Loc.GetString("rmc-payload-deployment-add-anyway"),
                Loc.GetString("rmc-payload-deployment-cancel"));
            _confirmation.AcceptButton.OnPressed += _ =>
            {
                _confirmation.Close();
                AddEntities(selectedEntries);
            };
            _confirmation.DenyButton.OnPressed += _ => _confirmation.Close();
            if (_popOutRoot != null)
                _popOutRoot.AddChild(_confirmation);

            _confirmation.OpenCentered();
            return;
        }

        AddEntities(selectedEntries);
    }

    private void AddEntities(IEnumerable<RMCPayloadDeploymentEntityEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (!ActiveManifest.Entities.ContainsKey(entry.Entity) &&
                ActiveManifest.PayloadCount() >= RMCPayloadDeploymentLimits.MaxPayload)
                break;

            foreach (var manifest in _manifests)
            {
                manifest.Entities.Remove(entry.Entity);
            }

            ActiveManifest.Entities[entry.Entity] = entry;
        }

        UpdateManifest();
        RebuildNearbyList();
        RebuildPlayerList();
    }

    private void AddSelectedPrototype()
    {
        if (_control.Prototypes.GetSelected().FirstOrDefault()?.Metadata is not EntProtoId prototype)
            return;

        var remaining = RMCPayloadDeploymentLimits.MaxPayload - ActiveManifest.PayloadCount();
        if (remaining <= 0)
            return;

        var quantity = Math.Min(_control.PrototypeQuantity.Value, remaining);
        ActiveManifest.Prototypes[prototype] = ActiveManifest.Prototypes.GetValueOrDefault(prototype) + quantity;
        UpdateManifest();
    }

    private void RemoveSelectedManifestEntry()
    {
        foreach (var selected in _control.Manifest.GetSelected().ToArray())
        {
            switch (selected.Metadata)
            {
                case NetEntity entity:
                    ActiveManifest.Entities.Remove(entity);
                    break;
                case EntProtoId prototype:
                    ActiveManifest.Prototypes.Remove(prototype);
                    break;
            }
        }

        UpdateManifest();
        RebuildNearbyList();
        RebuildPlayerList();
    }

    private void ClearManifestEntries()
    {
        ActiveManifest.Entities.Clear();
        ActiveManifest.Prototypes.Clear();
        UpdateManifest();
        RebuildNearbyList();
        RebuildPlayerList();
    }

    private void UpdateManifest()
    {
        var items = new List<RMCPayloadDeploymentListEntry>();
        foreach (var (entity, entry) in ActiveManifest.Entities)
        {
            var text = $"{entry.Name} [{entity}]";
            if (!string.IsNullOrWhiteSpace(entry.Role))
                text += $" — {entry.Role}";

            EntProtoId? prototype = null;
            if (_prototypes.HasIndex<EntityPrototype>(entry.Prototype))
                prototype = new EntProtoId(entry.Prototype);

            items.Add(new RMCPayloadDeploymentListEntry(text, entity, Prototype: prototype));
        }

        foreach (var (prototype, quantity) in ActiveManifest.Prototypes)
        {
            var name = _prototypes.TryIndex(prototype, out EntityPrototype? entityPrototype)
                ? entityPrototype.Name
                : prototype.Id;
            items.Add(new RMCPayloadDeploymentListEntry(
                $"{name} ({prototype}) ×{quantity}",
                prototype,
                Prototype: prototype));
        }

        _control.Manifest.SetItems(items);

        var total = ActiveManifest.PayloadCount();
        _control.ManifestSummary.Text = Loc.GetString("rmc-payload-deployment-manifest-summary",
            ("manifest", GetManifestLabel(_activeManifest, false)),
            ("payload", total),
            ("total", _manifests.Sum(manifest => manifest.PayloadCount())));
        var maximumPods = Math.Max(1, Math.Min(RMCPayloadDeploymentLimits.MaxOrbitalDrops, total));
        _control.PodCount.OverrideValue(Math.Min(_control.PodCount.Value, maximumPods));
        ActiveManifest.PodCount = _control.PodCount.Value;
        UpdateManifestOptions();
        ValidateManifestEntities();
    }

    private void ValidateManifestEntities()
    {
        var entities = _manifests
            .SelectMany(manifest => manifest.Entities.Keys)
            .Distinct()
            .ToList();
        if (entities.Count > 0)
            SendMessage(new RMCPayloadDeploymentValidateEntitiesMsg(entities));
    }

    private static bool ValidDuration(float value)
    {
        return value is >= 0 and <= RMCPayloadDeploymentLimits.MaxTimingSeconds;
    }

    private void UpdateLandingTileCount()
    {
        var radius = _control.LandingRadius.Value;
        var radiusSquared = radius * radius;
        var radiusBounds = (int) MathF.Ceiling(radius);
        var tiles = 0;
        for (var x = -radiusBounds; x <= radiusBounds; x++)
        {
            for (var y = -radiusBounds; y <= radiusBounds; y++)
            {
                if (x * x + y * y <= radiusSquared)
                    tiles++;
            }
        }

        _control.LandingTileCount.Text = Loc.GetString("rmc-payload-deployment-landing-tiles", ("tiles", tiles));
    }

    private void UpdateMaps(IReadOnlyList<RMCPayloadDeploymentMapEntry> maps)
    {
        _maps.Clear();
        _maps.AddRange(maps);
        _control.MapOptions.Clear();
        foreach (var map in _maps)
        {
            _control.MapOptions.AddItem(FormatMap(map.MapId));
        }

        MapId? defaultMap = null;
        if (_players.LocalEntity is { } player &&
            _entities.TryGetComponent(player, out TransformComponent? transform) &&
            _maps.Any(entry => entry.MapId == transform.MapID))
        {
            defaultMap = transform.MapID;
        }
        else if (_maps.Count > 0)
        {
            defaultMap = _maps[0].MapId;
        }

        foreach (var manifest in _manifests)
        {
            if (manifest.Map is not { } selected || _maps.All(entry => entry.MapId != selected))
                manifest.Map = defaultMap;
        }

        LoadActiveManifest();
        if (_initializeTarget)
        {
            _initializeTarget = false;
            SetCurrentPosition();
        }
    }

    private void UpdateCoordinateMode()
    {
        if (_control.MapOptions.SelectedId < 0 || _control.MapOptions.SelectedId >= _maps.Count)
            return;

        var hasPlanetCoordinates = _maps[_control.MapOptions.SelectedId].HasPlanetCoordinates;
        if (hasPlanetCoordinates)
        {
            _control.RawCoordinates.Disabled = false;
            _control.RawCoordinates.Pressed = ActiveManifest.RawCoordinates;
        }
        else
        {
            _control.RawCoordinates.Pressed = true;
            _control.RawCoordinates.Disabled = true;
            ActiveManifest.RawCoordinates = true;
        }
    }

    private void SetCurrentPosition()
    {
        if (_players.LocalEntity is not { } player ||
            !_entities.TryGetComponent(player, out TransformComponent? transform))
        {
            return;
        }

        var mapIndex = _maps.FindIndex(entry => entry.MapId == transform.MapID);
        if (mapIndex >= 0)
        {
            _control.MapOptions.Select(mapIndex);
            ActiveManifest.Map = transform.MapID;
            UpdateCoordinateMode();
            UpdateManifestOptions();
        }

        var position = _transform.GetMapCoordinates(player, transform).Position.Floored();
        if (mapIndex >= 0 && !_control.RawCoordinates.Pressed)
            position += _maps[mapIndex].CoordinateOffset;

        _control.MapX.Value = position.X;
        _control.MapY.Value = position.Y;
        ActiveManifest.Coordinates = position;
    }

    private bool ValidateManifests(IReadOnlyList<int> manifests)
    {
        SaveActiveManifest();
        if (manifests.Count == 0)
            return false;

        var totalPayload = manifests.Sum(index => _manifests[index].PayloadCount());
        if (totalPayload is <= 0 or > RMCPayloadDeploymentLimits.MaxPayload)
        {
            return false;
        }

        var orbitalDrops = 0;
        foreach (var index in manifests)
        {
            var manifest = _manifests[index];
            if (manifest.PayloadCount() <= 0 ||
                manifest.Map == null)
            {
                return false;
            }

            if (_deliveryType != RMCPayloadDeliveryType.Orbital)
                continue;

            if (manifest.UseDropPods)
            {
                if (manifest.PodCount <= 0 || manifest.PodCount > manifest.PayloadCount())
                    return false;

                orbitalDrops += manifest.PodCount;
            }
            else
            {
                orbitalDrops += manifest.PayloadCount();
            }
        }

        return orbitalDrops <= RMCPayloadDeploymentLimits.MaxOrbitalDrops;
    }

    private void ConfirmLaunch(bool selectedOnly)
    {
        if (_pendingManifests != null)
            return;

        var manifests = selectedOnly
            ? new List<int> { _activeManifest }
            : Enumerable.Range(0, _manifests.Count).ToList();
        if (!ValidateManifests(manifests))
        {
            _control.Result.Text = Loc.GetString("rmc-payload-deployment-result-empty");
            return;
        }

        var deliveryType = _deliveryType;
        var pending = manifests
            .Select(index => new PendingManifest(
                _manifests[index],
                _manifests[index].Clone(),
                GetManifestLabel(index, false)))
            .ToList();
        var payload = pending.Sum(entry => entry.Snapshot.PayloadCount());
        var selection = pending.Count == 1
            ? pending[0].Label
            : Loc.GetString("rmc-payload-deployment-all-manifests", ("count", manifests.Count));
        _confirmation?.Close();
        var confirmation = new ConfirmationWindow();
        _confirmation = confirmation;
        confirmation.Setup(
            Loc.GetString("rmc-payload-deployment-confirm-title"),
            Loc.GetString("rmc-payload-deployment-confirm-text",
                ("payload", payload),
                ("selection", selection),
                ("type", Loc.GetString(deliveryType == RMCPayloadDeliveryType.Orbital
                    ? "rmc-payload-deployment-orbital"
                    : "rmc-payload-deployment-paradrop"))),
            Loc.GetString("rmc-payload-deployment-confirm"),
            Loc.GetString("rmc-payload-deployment-cancel"));
        confirmation.OnClose += () =>
        {
            if (_confirmation == confirmation)
                _confirmation = null;

            if (_pendingManifests == null)
                SetInputBlocked(false);
        };
        confirmation.AcceptButton.OnPressed += _ =>
        {
            confirmation.Close();
            SendLaunch(deliveryType, pending);
        };
        confirmation.DenyButton.OnPressed += _ => confirmation.Close();
        if (_popOutRoot != null)
            _popOutRoot.AddChild(confirmation);

        SetInputBlocked(true);
        confirmation.OpenCentered();
    }

    private void SendLaunch(RMCPayloadDeliveryType deliveryType, List<PendingManifest> pending)
    {
        _pendingManifests = pending;
        SetInputBlocked(true);
        SetLaunchButtonsDisabled(true);
        if (deliveryType == RMCPayloadDeliveryType.Orbital)
        {
            var manifests = new List<RMCOrbitalDropManifestMsg>(pending.Count);
            foreach (var pendingManifest in pending)
            {
                var manifest = pendingManifest.Snapshot;
                manifests.Add(new RMCOrbitalDropManifestMsg(
                    manifest.Entities.Keys.ToList(),
                    manifest.Prototypes
                        .Select(entry => new RMCDropPrototypePayload(entry.Key, entry.Value))
                        .ToList(),
                    manifest.Map!.Value,
                    manifest.Coordinates,
                    manifest.LandingRadius,
                    manifest.UseDropPods,
                    manifest.PodCount,
                    manifest.ArrivalDelay,
                    manifest.DropDuration,
                    manifest.OpenDelay,
                    manifest.LaunchInterval,
                    manifest.ArrivalInterval,
                    manifest.ArrivalIntervalVariation,
                    manifest.UseParachute,
                    manifest.ShowLandingWarning,
                    manifest.RawCoordinates,
                    manifest.IgnoreParadropRestrictions));
            }

            SendMessage(new RMCOrbitalDropBatchLaunchMsg(manifests));
            return;
        }

        var paraDropManifests = new List<RMCParaDropManifestMsg>(pending.Count);
        foreach (var pendingManifest in pending)
        {
            var manifest = pendingManifest.Snapshot;
            paraDropManifests.Add(new RMCParaDropManifestMsg(
                manifest.Entities.Keys.ToList(),
                manifest.Prototypes
                    .Select(entry => new RMCDropPrototypePayload(entry.Key, entry.Value))
                    .ToList(),
                manifest.Map!.Value,
                manifest.Coordinates,
                manifest.LandingRadius,
                manifest.ArrivalDelay,
                manifest.DropDuration,
                manifest.LaunchInterval,
                manifest.ArrivalInterval,
                manifest.ArrivalIntervalVariation,
                manifest.RawCoordinates,
                manifest.IgnoreParadropRestrictions));
        }

        SendMessage(new RMCParaDropBatchLaunchMsg(paraDropManifests));
    }

    private void SetLaunchButtonsDisabled(bool disabled)
    {
        _control.LaunchSelected.Disabled = disabled;
        _control.LaunchAll.Disabled = disabled;
    }

    private void SetInputBlocked(bool blocked)
    {
        _control.InputBlocker.Visible = blocked;
        if (blocked)
            _uiManager.ReleaseKeyboardFocus();
    }

    private void Refresh(int radius)
    {
        SaveActiveManifest();
        SendMessage(new RMCPayloadDeploymentRefreshMsg(radius));
    }

    private void SendClosedMessage()
    {
        SendMessage(new CloseEuiMessage());
    }

    private sealed class PendingManifest(
        RMCPayloadDeploymentManifest original,
        RMCPayloadDeploymentManifest snapshot,
        string label)
    {
        public readonly RMCPayloadDeploymentManifest Original = original;
        public readonly RMCPayloadDeploymentManifest Snapshot = snapshot;
        public readonly string Label = label;
    }
}
