using System.Linq;
using Content.Client._RMC14.UserInterface;
using Content.Client.Eui;
using Content.Shared._RMC14.Admin.Utility;
using Content.Shared._RMC14.Dropship.Utility;
using Content.Shared._RMC14.ParaDrop;
using Content.Shared.Eui;
using Content.Shared.Item;
using Content.Shared.Prototypes;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Admin.Utility;

[UsedImplicitly]
public sealed class RMCPayloadDeploymentEui : BaseEui
{
    private const int MaxNearbyRadius = 50;
    private const int MaxManifestNameLength = 32;
    private const int MaxPrototypeResults = 250;

    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private readonly List<RMCPayloadDeploymentManifest> _manifests = [];
    private readonly List<RMCPayloadDeploymentMapEntry> _maps = [];
    private readonly List<EntityPrototype> _prototypeCatalog = [];

    private SharedTransformSystem _transform = default!;
    private RMCPayloadDeploymentDraftSystem _draft = default!;
    private RMCPayloadDeploymentWindow _window = default!;
    private List<RMCPayloadDeploymentEntityEntry> _nearby = [];
    private List<RMCPayloadDeploymentEntityEntry> _playerControlled = [];
    private ConfirmationWindow? _confirmation;
    private RMCPayloadDeliveryType _deliveryType;
    private int _activeManifest;
    private bool _initializeTarget = true;
    private bool _loadingManifest;
    private List<int>? _pendingManifests;

    private RMCPayloadDeploymentManifest ActiveManifest => _manifests[_activeManifest];

    public override void Opened()
    {
        _transform = _entities.System<SharedTransformSystem>();
        _draft = _entities.System<RMCPayloadDeploymentDraftSystem>();
        _window = new RMCPayloadDeploymentWindow();
        _window.OnClose += SendClosedMessage;

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

        _window.DropType.AddItem(Loc.GetString("rmc-payload-deployment-orbital"), (int) RMCPayloadDeliveryType.Orbital);
        _window.DropType.AddItem(Loc.GetString("rmc-payload-deployment-paradrop"), (int) RMCPayloadDeliveryType.ParaDrop);
        _window.DropType.SelectId((int) _deliveryType);

        _window.NearbyRadius.IsValid = value => value is >= 1 and <= MaxNearbyRadius;
        _window.PrototypeQuantity.IsValid = value => value is >= 1 and <= RMCPayloadDeploymentLimits.MaxPayload;
        _window.LandingRadius.IsValid = value =>
            value is >= 0 and <= RMCPayloadDeploymentLimits.MaxLandingRadius &&
            MathF.Abs(value * 2 - MathF.Round(value * 2)) < 0.001f;
        _window.PodCount.IsValid = value => value >= 1 &&
            value <= Math.Max(1, Math.Min(RMCPayloadDeploymentLimits.MaxPods, ActiveManifest.PayloadCount()));
        _window.LaunchInterval.IsValid = ValidDuration;
        _window.ArrivalDelay.IsValid = ValidDuration;
        _window.ArrivalInterval.IsValid = ValidDuration;
        _window.ArrivalIntervalVariation.IsValid = ValidDuration;
        _window.DropDuration.IsValid = ValidDuration;
        _window.OpenDelay.IsValid = ValidDuration;
        _window.ManifestName.IsValid = value => value.Length <= MaxManifestNameLength;

        _window.RefreshNearby.OnPressed += _ => Refresh(_window.NearbyRadius.Value);
        _window.SelectAllNearby.OnPressed += _ => SelectAllItems(_window.NearbyEntities);
        _window.AddNearby.OnPressed += _ => AddSelectedEntities(_window.NearbyEntities, true);
        _window.AddPlayer.OnPressed += _ => AddSelectedEntities(_window.PlayerEntities, false);
        _window.AddPrototype.OnPressed += _ => AddSelectedPrototype();
        _window.RemoveManifestEntry.OnPressed += _ => RemoveSelectedManifestEntry();
        _window.ClearManifest.OnPressed += _ => ClearManifestEntries();
        _window.UseCurrentPosition.OnPressed += _ => SetCurrentPosition();
        _window.AddManifest.OnPressed += _ => AddManifest();
        _window.RemoveManifest.OnPressed += _ => RemoveManifest();
        _window.LaunchSelected.OnPressed += _ => ConfirmLaunch(true);
        _window.LaunchAll.OnPressed += _ => ConfirmLaunch(false);

        _window.NearbySearch.OnTextChanged += _ => RebuildNearbyList();
        _window.PlayerSearch.OnTextChanged += _ => RebuildPlayerList();
        _window.PrototypeSearch.OnTextChanged += _ => RebuildPrototypeList();
        _window.LandingRadius.OnValueChanged += _ => UpdateLandingTileCount();
        _window.ManifestName.OnTextChanged += args =>
        {
            if (_loadingManifest)
                return;

            ActiveManifest.Name = args.Text;
            UpdateManifestOptions();
            RebuildNearbyList();
            RebuildPlayerList();
        };
        _window.DropType.OnItemSelected += OnDropTypeSelected;
        _window.ManifestOptions.OnItemSelected += OnManifestSelected;
        _window.MapOptions.OnItemSelected += args =>
        {
            _window.MapOptions.SelectId(args.Id);
            if (!_loadingManifest && args.Id >= 0 && args.Id < _maps.Count)
            {
                ActiveManifest.Map = _maps[args.Id].MapId;
                UpdateManifestOptions();
            }

            UpdateCoordinateMode();
        };
        _window.RawCoordinates.OnToggled += args =>
        {
            if (_loadingManifest ||
                _window.MapOptions.SelectedId < 0 ||
                _window.MapOptions.SelectedId >= _maps.Count ||
                !_maps[_window.MapOptions.SelectedId].HasPlanetCoordinates)
            {
                return;
            }

            var map = _maps[_window.MapOptions.SelectedId];
            var coordinates = new Vector2i(_window.MapX.Value, _window.MapY.Value);
            coordinates += args.Pressed
                ? -map.CoordinateOffset
                : map.CoordinateOffset;

            _window.MapX.Value = coordinates.X;
            _window.MapY.Value = coordinates.Y;
            ActiveManifest.Coordinates = coordinates;
            ActiveManifest.RawCoordinates = args.Pressed;
        };

        BuildPrototypeCatalog();
        UpdateModeControls();
        LoadActiveManifest();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        SaveActiveManifest();
        _draft.Save(_deliveryType, _activeManifest, _manifests);
        _confirmation?.Close();
        _confirmation = null;
        _window.OnClose -= SendClosedMessage;
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not RMCPayloadDeploymentEuiState deploymentState)
            return;

        _nearby = deploymentState.Nearby;
        _playerControlled = deploymentState.PlayerControlled;
        _window.NearbyRadius.Value = (int) deploymentState.NearbyRadius;
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
        SetLaunchButtonsDisabled(false);
        if (result.Failure == RMCPayloadDeploymentFailure.None)
        {
            foreach (var manifest in pending)
            {
                if (manifest >= 0 && manifest < _manifests.Count)
                    _manifests[manifest].Entities.Clear();
            }

            _window.Result.Text = Loc.GetString("rmc-payload-deployment-result-success", ("manifests", pending.Count));
            UpdateManifest();
            RebuildNearbyList();
            RebuildPlayerList();
            _draft.Save(_deliveryType, _activeManifest, _manifests);
            return;
        }

        var failedManifest = result.FailedManifest >= 0 && result.FailedManifest < pending.Count
            ? pending[result.FailedManifest]
            : -1;
        var manifestLabel = failedManifest >= 0
            ? GetManifestLabel(failedManifest, false)
            : Loc.GetString("rmc-payload-deployment-unknown-manifest");
        if (failedManifest >= 0)
        {
            SaveActiveManifest();
            _activeManifest = failedManifest;
            LoadActiveManifest();
        }

        _window.Result.Text = result.Failure switch
        {
            RMCPayloadDeploymentFailure.InvalidPayload => Loc.GetString("rmc-payload-deployment-result-invalid-payload", ("manifest", manifestLabel)),
            RMCPayloadDeploymentFailure.InvalidPrototype => Loc.GetString("rmc-payload-deployment-result-invalid-prototype", ("manifest", manifestLabel)),
            RMCPayloadDeploymentFailure.InvalidSettings => Loc.GetString("rmc-payload-deployment-result-invalid-settings", ("manifest", manifestLabel)),
            RMCPayloadDeploymentFailure.InvalidTarget => Loc.GetString("rmc-payload-deployment-result-invalid-target", ("manifest", manifestLabel)),
            RMCPayloadDeploymentFailure.InsufficientLandingTiles => Loc.GetString(
                "rmc-payload-deployment-result-insufficient-tiles",
                ("manifest", manifestLabel),
                ("requested", result.RequestedLandingTiles),
                ("viable", result.ViableLandingTiles)),
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
        _window.DropType.SelectId(args.Id);
        var removed = RemoveInvalidParaDropPrototypes();
        BuildPrototypeCatalog();
        UpdateModeControls();
        LoadActiveManifest();
        if (removed > 0)
        {
            _window.Result.Text = Loc.GetString("rmc-payload-deployment-removed-prototypes",
                ("count", removed));
        }
    }

    private void OnManifestSelected(OptionButton.ItemSelectedEventArgs args)
    {
        if (_loadingManifest || args.Id < 0 || args.Id >= _manifests.Count)
            return;

        SaveActiveManifest();
        _activeManifest = args.Id;
        _window.ManifestOptions.SelectId(args.Id);
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
            var previousDefault = Loc.GetString("rmc-payload-deployment-manifest-number",
                ("number", i + 2));
            if (_manifests[i].Name == previousDefault)
            {
                _manifests[i].Name = Loc.GetString("rmc-payload-deployment-manifest-number",
                    ("number", i + 1));
            }
        }

        _activeManifest = Math.Min(_activeManifest, _manifests.Count - 1);
        LoadActiveManifest();
        RebuildNearbyList();
        RebuildPlayerList();
    }

    private void UpdateManifestOptions()
    {
        _loadingManifest = true;
        _window.ManifestOptions.Clear();
        for (var i = 0; i < _manifests.Count; i++)
        {
            _window.ManifestOptions.AddItem(GetManifestLabel(i, true), i);
        }

        _window.ManifestOptions.SelectId(_activeManifest);
        _window.RemoveManifest.Disabled = _manifests.Count <= 1;
        _window.AddManifest.Disabled = _manifests.Count >= RMCPayloadDeploymentLimits.MaxBatchRequests;
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
        manifest.Name = _window.ManifestName.Text;
        if (_window.MapOptions.SelectedId >= 0 && _window.MapOptions.SelectedId < _maps.Count)
            manifest.Map = _maps[_window.MapOptions.SelectedId].MapId;

        manifest.Coordinates = new Vector2i(_window.MapX.Value, _window.MapY.Value);
        manifest.LandingRadius = _window.LandingRadius.Value;
        manifest.PodCount = _window.PodCount.Value;
        manifest.ArrivalDelay = _window.ArrivalDelay.Value;
        manifest.DropDuration = _window.DropDuration.Value;
        manifest.OpenDelay = _window.OpenDelay.Value;
        manifest.LaunchInterval = _window.LaunchInterval.Value;
        manifest.ArrivalInterval = _window.ArrivalInterval.Value;
        manifest.ArrivalIntervalVariation = _window.ArrivalIntervalVariation.Value;
        manifest.UseParachute = _window.UseParachute.Pressed;
        manifest.RawCoordinates = _window.RawCoordinates.Pressed;
        manifest.IgnoreParadropRestrictions = _window.IgnoreParadropRestrictions.Pressed;
    }

    private void LoadActiveManifest()
    {
        if (_manifests.Count == 0)
            return;

        _loadingManifest = true;
        var manifest = ActiveManifest;
        _window.ManifestName.Text = manifest.Name;
        var mapIndex = manifest.Map is { } map
            ? _maps.FindIndex(entry => entry.MapId == map)
            : -1;
        if (mapIndex < 0 && _maps.Count > 0)
        {
            mapIndex = 0;
            manifest.Map = _maps[0].MapId;
        }

        if (mapIndex >= 0)
            _window.MapOptions.Select(mapIndex);

        _window.MapX.Value = manifest.Coordinates.X;
        _window.MapY.Value = manifest.Coordinates.Y;
        _window.LandingRadius.Value = manifest.LandingRadius;
        _window.PodCount.Value = Math.Max(1,
            Math.Min(manifest.PodCount, Math.Max(1, manifest.PayloadCount())));
        _window.ArrivalDelay.Value = manifest.ArrivalDelay;
        _window.DropDuration.Value = manifest.DropDuration;
        _window.OpenDelay.Value = manifest.OpenDelay;
        _window.LaunchInterval.Value = manifest.LaunchInterval;
        _window.ArrivalInterval.Value = manifest.ArrivalInterval;
        _window.ArrivalIntervalVariation.Value = manifest.ArrivalIntervalVariation;
        _window.UseParachute.Pressed = manifest.UseParachute;
        _window.IgnoreParadropRestrictions.Pressed = manifest.IgnoreParadropRestrictions;
        UpdateCoordinateMode();
        UpdateLandingTileCount();
        _loadingManifest = false;
        UpdateManifest();
    }

    private void UpdateModeControls()
    {
        var orbital = _deliveryType == RMCPayloadDeliveryType.Orbital;
        _window.PodCountRow.Visible = orbital;
        _window.OpenDelayRow.Visible = orbital;
        _window.UseParachute.Visible = orbital;
    }

    private int RemoveInvalidParaDropPrototypes()
    {
        if (_deliveryType != RMCPayloadDeliveryType.ParaDrop)
            return 0;

        var removed = 0;
        foreach (var manifest in _manifests)
        {
            foreach (var (prototype, quantity) in manifest.Prototypes.ToArray())
            {
                if (_prototypes.TryIndex(prototype, out EntityPrototype? entityPrototype) &&
                    entityPrototype.HasComponent<ParaDroppableComponent>())
                {
                    continue;
                }

                removed += quantity;
                manifest.Prototypes.Remove(prototype);
            }
        }

        return removed;
    }

    private void BuildPrototypeCatalog()
    {
        _prototypeCatalog.Clear();
        var categoryFilter = _configuration.GetCVar(CVars.EntitiesCategoryFilter);
        _prototypes.TryIndex<EntityCategoryPrototype>(categoryFilter, out var filter);

        foreach (var prototype in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            var validType = _deliveryType switch
            {
                RMCPayloadDeliveryType.Orbital => prototype.HasComponent<ItemComponent>(),
                RMCPayloadDeliveryType.ParaDrop => prototype.HasComponent<ParaDroppableComponent>(),
                _ => false,
            };
            if (prototype.Abstract ||
                prototype.HideSpawnMenu ||
                !validType ||
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
        var search = _window.PrototypeSearch.Text.Trim();
        _window.Prototypes.Clear();
        var shown = 0;
        foreach (var prototype in _prototypeCatalog)
        {
            if (!MatchesPrototype(prototype, search))
                continue;

            var prototypeName = string.IsNullOrWhiteSpace(prototype.Name) ? prototype.ID : prototype.Name;
            if (!string.IsNullOrWhiteSpace(prototype.EditorSuffix))
                prototypeName += $" [{prototype.EditorSuffix}]";

            _window.Prototypes.Add(new ItemList.Item(_window.Prototypes)
            {
                Text = prototypeName,
                TooltipText = prototype.Description,
                Metadata = new EntProtoId(prototype.ID),
            });

            if (++shown >= MaxPrototypeResults)
                break;
        }
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
        PopulateEntityList(_window.NearbyEntities, _nearby, _window.NearbySearch.Text, false);
    }

    private void RebuildPlayerList()
    {
        PopulateEntityList(_window.PlayerEntities, _playerControlled, _window.PlayerSearch.Text, true);
    }

    private void PopulateEntityList(
        ItemList list,
        IEnumerable<RMCPayloadDeploymentEntityEntry> entries,
        string search,
        bool showMap)
    {
        list.Clear();
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

            list.Add(new ItemList.Item(list)
            {
                Text = text,
                TooltipText = entry.Prototype,
                Metadata = entry,
            });
        }
    }

    private void AddSelectedEntities(ItemList list, bool confirmCrossMap)
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

    private static void SelectAllItems(ItemList list)
    {
        foreach (var item in list)
        {
            if (item.Selectable && !item.Disabled)
                item.Selected = true;
        }
    }

    private void AddSelectedPrototype()
    {
        if (_window.Prototypes.GetSelected().FirstOrDefault()?.Metadata is not EntProtoId prototype)
            return;

        var remaining = RMCPayloadDeploymentLimits.MaxPayload - ActiveManifest.PayloadCount();
        if (remaining <= 0)
            return;

        var quantity = Math.Min(_window.PrototypeQuantity.Value, remaining);
        ActiveManifest.Prototypes[prototype] = ActiveManifest.Prototypes.GetValueOrDefault(prototype) + quantity;
        UpdateManifest();
    }

    private void RemoveSelectedManifestEntry()
    {
        foreach (var selected in _window.Manifest.GetSelected().ToArray())
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
        _window.Manifest.Clear();
        foreach (var (entity, entry) in ActiveManifest.Entities)
        {
            _window.Manifest.Add(new ItemList.Item(_window.Manifest)
            {
                Text = $"{entry.Name} [{entity}]",
                Metadata = entity,
            });
        }

        foreach (var (prototype, quantity) in ActiveManifest.Prototypes)
        {
            var name = _prototypes.TryIndex(prototype, out EntityPrototype? entityPrototype)
                ? entityPrototype.Name
                : prototype.Id;
            _window.Manifest.Add(new ItemList.Item(_window.Manifest)
            {
                Text = $"{name} ({prototype}) ×{quantity}",
                Metadata = prototype,
            });
        }

        var total = ActiveManifest.PayloadCount();
        _window.ManifestSummary.Text = Loc.GetString("rmc-payload-deployment-manifest-summary",
            ("manifest", GetManifestLabel(_activeManifest, false)),
            ("payload", total),
            ("total", TotalPayloadCount()));
        var maximumPods = Math.Max(1, Math.Min(RMCPayloadDeploymentLimits.MaxPods, total));
        if (_window.PodCount.Value > maximumPods)
            _window.PodCount.Value = maximumPods;
        ActiveManifest.PodCount = _window.PodCount.Value;
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

    private int TotalPayloadCount()
    {
        return _manifests.Sum(manifest => manifest.PayloadCount());
    }

    private static bool ValidDuration(float value)
    {
        return value is >= 0 and <= RMCPayloadDeploymentLimits.MaxTimingSeconds;
    }

    private void UpdateLandingTileCount()
    {
        var radius = _window.LandingRadius.Value;
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

        _window.LandingTileCount.Text = Loc.GetString("rmc-payload-deployment-landing-tiles", ("tiles", tiles));
    }

    private void UpdateMaps(IReadOnlyList<RMCPayloadDeploymentMapEntry> maps)
    {
        _maps.Clear();
        _maps.AddRange(maps);
        _window.MapOptions.Clear();
        foreach (var map in _maps)
        {
            _window.MapOptions.AddItem(FormatMap(map.MapId));
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
        if (_window.MapOptions.SelectedId < 0 || _window.MapOptions.SelectedId >= _maps.Count)
            return;

        var hasPlanetCoordinates = _maps[_window.MapOptions.SelectedId].HasPlanetCoordinates;
        if (hasPlanetCoordinates)
        {
            _window.RawCoordinates.Disabled = false;
            _window.RawCoordinates.Pressed = ActiveManifest.RawCoordinates;
        }
        else
        {
            _window.RawCoordinates.Pressed = true;
            _window.RawCoordinates.Disabled = true;
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
            _window.MapOptions.Select(mapIndex);
            ActiveManifest.Map = transform.MapID;
            UpdateCoordinateMode();
            UpdateManifestOptions();
        }

        var position = _transform.GetMapCoordinates(player, transform).Position.Floored();
        if (mapIndex >= 0 && !_window.RawCoordinates.Pressed)
            position += _maps[mapIndex].CoordinateOffset;

        _window.MapX.Value = position.X;
        _window.MapY.Value = position.Y;
        ActiveManifest.Coordinates = position;
    }

    private bool ValidateManifests(IReadOnlyList<int> manifests)
    {
        SaveActiveManifest();
        if (manifests.Count == 0)
            return false;

        var totalPayload = manifests.Sum(index => _manifests[index].PayloadCount());
        if (totalPayload <= 0 || totalPayload > RMCPayloadDeploymentLimits.MaxPayload)
        {
            return false;
        }

        var pods = 0;
        foreach (var index in manifests)
        {
            var manifest = _manifests[index];
            if (manifest.PayloadCount() <= 0 ||
                manifest.Map == null)
            {
                return false;
            }

            if (_deliveryType == RMCPayloadDeliveryType.Orbital)
            {
                if (manifest.PodCount <= 0 || manifest.PodCount > manifest.PayloadCount())
                    return false;

                pods += manifest.PodCount;
            }
        }

        return pods <= RMCPayloadDeploymentLimits.MaxPods;
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
            _window.Result.Text = Loc.GetString("rmc-payload-deployment-result-empty");
            return;
        }

        var payload = manifests.Sum(index => _manifests[index].PayloadCount());
        var selection = manifests.Count == 1
            ? GetManifestLabel(manifests[0], false)
            : Loc.GetString("rmc-payload-deployment-all-manifests", ("count", manifests.Count));
        _confirmation?.Close();
        _confirmation = new ConfirmationWindow();
        _confirmation.Setup(
            Loc.GetString("rmc-payload-deployment-confirm-title"),
            Loc.GetString("rmc-payload-deployment-confirm-text",
                ("payload", payload),
                ("selection", selection),
                ("type", Loc.GetString(_deliveryType == RMCPayloadDeliveryType.Orbital
                    ? "rmc-payload-deployment-orbital"
                    : "rmc-payload-deployment-paradrop"))),
            Loc.GetString("rmc-payload-deployment-confirm"),
            Loc.GetString("rmc-payload-deployment-cancel"));
        _confirmation.AcceptButton.OnPressed += _ =>
        {
            _confirmation.Close();
            SendLaunch(manifests);
        };
        _confirmation.DenyButton.OnPressed += _ => _confirmation.Close();
        _confirmation.OpenCentered();
    }

    private void SendLaunch(List<int> manifestIndices)
    {
        _pendingManifests = manifestIndices;
        SetLaunchButtonsDisabled(true);
        if (_deliveryType == RMCPayloadDeliveryType.Orbital)
        {
            var manifests = new List<RMCOrbitalDropManifestMsg>(manifestIndices.Count);
            foreach (var index in manifestIndices)
            {
                var manifest = _manifests[index];
                manifests.Add(new RMCOrbitalDropManifestMsg(
                    manifest.Entities.Keys.ToList(),
                    manifest.Prototypes
                        .Select(entry => new RMCDropPrototypePayload(entry.Key, entry.Value))
                        .ToList(),
                    manifest.Map!.Value,
                    manifest.Coordinates,
                    manifest.LandingRadius,
                    manifest.PodCount,
                    manifest.ArrivalDelay,
                    manifest.DropDuration,
                    manifest.OpenDelay,
                    manifest.LaunchInterval,
                    manifest.ArrivalInterval,
                    manifest.ArrivalIntervalVariation,
                    manifest.UseParachute,
                    manifest.RawCoordinates,
                    manifest.IgnoreParadropRestrictions));
            }

            SendMessage(new RMCOrbitalDropBatchLaunchMsg(manifests));
            return;
        }

        var paraDropManifests = new List<RMCParaDropManifestMsg>(manifestIndices.Count);
        foreach (var index in manifestIndices)
        {
            var manifest = _manifests[index];
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
        _window.LaunchSelected.Disabled = disabled;
        _window.LaunchAll.Disabled = disabled;
    }

    private void Refresh(float radius)
    {
        SaveActiveManifest();
        SendMessage(new RMCPayloadDeploymentRefreshMsg(radius));
    }

    private void SendClosedMessage()
    {
        SendMessage(new CloseEuiMessage());
    }
}
