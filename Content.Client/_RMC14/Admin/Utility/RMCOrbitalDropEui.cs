using System.Linq;
using Content.Client._RMC14.UserInterface;
using Content.Client.Eui;
using Content.Shared._RMC14.Admin.Utility;
using Content.Shared._RMC14.Dropship.Utility;
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
public sealed class RMCOrbitalDropEui : BaseEui
{
    private const int MaxNearbyRadius = 50;
    private const int MaxPrototypeResults = 250;

    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private readonly List<RMCOrbitalDropMapEntry> _maps = [];
    private readonly List<EntityPrototype> _prototypeCatalog = [];
    private readonly Dictionary<NetEntity, RMCOrbitalDropEntityEntry> _entityManifest = new();
    private readonly Dictionary<EntProtoId, int> _prototypeManifest = new();

    private SharedTransformSystem _transform = default!;
    private RMCOrbitalDropWindow _window = default!;
    private List<RMCOrbitalDropEntityEntry> _nearby = [];
    private List<RMCOrbitalDropEntityEntry> _playerControlled = [];
    private ConfirmationWindow? _confirmation;
    private bool _rawCoordinatesOnPlanet;

    public override void Opened()
    {
        _transform = _entities.System<SharedTransformSystem>();
        _window = new RMCOrbitalDropWindow();
        _window.OnClose += SendClosedMessage;

        _window.NearbyRadius.IsValid = value => value is >= 1 and <= MaxNearbyRadius;
        _window.PrototypeQuantity.IsValid = value => value is >= 1 and <= RMCOrbitalDropRequest.MaxPayload;
        _window.LandingRadius.IsValid = value => value is >= 0 and <= RMCOrbitalDropRequest.MaxLandingRadius;
        _window.PodCount.IsValid = value => value >= 1 &&
            value <= Math.Max(1, Math.Min(RMCOrbitalDropRequest.MaxPods, ManifestPayloadCount()));
        _window.LaunchInterval.IsValid = ValidDuration;
        _window.ArrivalDelay.IsValid = ValidDuration;
        _window.DropInterval.IsValid = ValidDuration;
        _window.DropIntervalVariation.IsValid = ValidDuration;
        _window.DropDuration.IsValid = ValidDuration;
        _window.OpenDelay.IsValid = ValidDuration;

        _window.RefreshNearby.OnPressed += _ => Refresh(_window.NearbyRadius.Value);
        _window.SelectAllNearby.OnPressed += _ => SelectAllItems(_window.NearbyEntities);
        _window.AddNearby.OnPressed += _ => AddSelectedEntities(_window.NearbyEntities);
        _window.AddPlayer.OnPressed += _ => AddSelectedEntities(_window.PlayerEntities);
        _window.AddPrototype.OnPressed += _ => AddSelectedPrototype();
        _window.RemoveManifestEntry.OnPressed += _ => RemoveSelectedManifestEntry();
        _window.ClearManifest.OnPressed += _ => ClearManifestEntries();
        _window.UseCurrentPosition.OnPressed += _ => SetCurrentPosition();
        _window.Launch.OnPressed += _ => ConfirmLaunch();

        _window.NearbySearch.OnTextChanged += _ => RebuildNearbyList();
        _window.PlayerSearch.OnTextChanged += _ => RebuildPlayerList();
        _window.PrototypeSearch.OnTextChanged += _ => RebuildPrototypeList();
        _window.MapOptions.OnItemSelected += args =>
        {
            _window.MapOptions.SelectId(args.Id);
            UpdateCoordinateMode();
        };
        _window.RawCoordinates.OnToggled += args =>
        {
            if (_window.MapOptions.SelectedId >= 0 &&
                _window.MapOptions.SelectedId < _maps.Count &&
                _maps[_window.MapOptions.SelectedId].HasPlanetCoordinates)
            {
                _rawCoordinatesOnPlanet = args.Pressed;
            }
        };

        BuildPrototypeCatalog();
        UpdateManifest();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _confirmation?.Close();
        _confirmation = null;
        _window.OnClose -= SendClosedMessage;
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not RMCOrbitalDropEuiState orbitalDropState)
            return;

        _nearby = orbitalDropState.Nearby;
        _playerControlled = orbitalDropState.PlayerControlled;
        _window.NearbyRadius.Value = (int) orbitalDropState.NearbyRadius;
        UpdateMaps(orbitalDropState.Maps);
        RebuildNearbyList();
        RebuildPlayerList();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        if (msg is not RMCOrbitalDropResultMsg result)
            return;

        _window.Result.Text = result.Failure switch
        {
            RMCOrbitalDropFailure.None => Loc.GetString("rmc-orbital-drop-result-success",
                ("pods", result.RequestedLandingTiles)),
            RMCOrbitalDropFailure.InsufficientLandingTiles => Loc.GetString(
                "rmc-orbital-drop-result-insufficient-tiles",
                ("requested", result.RequestedLandingTiles),
                ("viable", result.ViableLandingTiles)),
            _ => Loc.GetString($"rmc-orbital-drop-result-{result.Failure.ToString().ToLowerInvariant()}"),
        };
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
                !prototype.HasComponent<ItemComponent>() ||
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
        PopulateEntityList(_window.NearbyEntities, _nearby, _window.NearbySearch.Text);
    }

    private void RebuildPlayerList()
    {
        PopulateEntityList(_window.PlayerEntities, _playerControlled, _window.PlayerSearch.Text);
    }

    private static void PopulateEntityList(
        ItemList list,
        IEnumerable<RMCOrbitalDropEntityEntry> entries,
        string search)
    {
        list.Clear();
        foreach (var entry in entries)
        {
            var filtering = $"{entry.Name} {entry.Prototype} {entry.Entity} {entry.Map}";
            if (!string.IsNullOrWhiteSpace(search) &&
                !filtering.Contains(search, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var distance = entry.Distance >= 0 ? $" — {entry.Distance:0.0}m" : string.Empty;
            list.Add(new ItemList.Item(list)
            {
                Text = $"{entry.Name} [{entry.Entity}] — {entry.Map}{distance}",
                TooltipText = entry.Prototype,
                Metadata = entry,
            });
        }
    }

    private void AddSelectedEntities(ItemList list)
    {
        foreach (var selected in list.GetSelected())
        {
            if (selected.Metadata is RMCOrbitalDropEntityEntry entry)
                _entityManifest[entry.Entity] = entry;
        }

        UpdateManifest();
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

        var quantity = _window.PrototypeQuantity.Value;
        _prototypeManifest[prototype] = Math.Min(
            RMCOrbitalDropRequest.MaxPayload,
            _prototypeManifest.GetValueOrDefault(prototype) + quantity);
        UpdateManifest();
    }

    private void RemoveSelectedManifestEntry()
    {
        foreach (var selected in _window.Manifest.GetSelected().ToArray())
        {
            switch (selected.Metadata)
            {
                case NetEntity entity:
                    _entityManifest.Remove(entity);
                    break;
                case EntProtoId prototype:
                    _prototypeManifest.Remove(prototype);
                    break;
            }
        }

        UpdateManifest();
    }

    private void ClearManifestEntries()
    {
        _entityManifest.Clear();
        _prototypeManifest.Clear();
        UpdateManifest();
    }

    private void UpdateManifest()
    {
        _window.Manifest.Clear();
        foreach (var (entity, entry) in _entityManifest)
        {
            _window.Manifest.Add(new ItemList.Item(_window.Manifest)
            {
                Text = $"{entry.Name} [{entity}]",
                Metadata = entity,
            });
        }

        foreach (var (prototype, quantity) in _prototypeManifest)
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

        var total = ManifestPayloadCount();
        _window.ManifestSummary.Text = Loc.GetString("rmc-orbital-drop-manifest-summary", ("entities", total));
        var maximumPods = Math.Max(1, Math.Min(RMCOrbitalDropRequest.MaxPods, total));
        if (_window.PodCount.Value > maximumPods)
            _window.PodCount.Value = maximumPods;
    }

    private int ManifestPayloadCount()
    {
        return _entityManifest.Count + _prototypeManifest.Values.Sum();
    }

    private static bool ValidDuration(float value)
    {
        return value is >= 0 and <= RMCOrbitalDropRequest.MaxTimingSeconds;
    }

    private void UpdateMaps(IReadOnlyList<RMCOrbitalDropMapEntry> maps)
    {
        MapId? selectedMap = null;
        if (_window.MapOptions.SelectedId >= 0 && _window.MapOptions.SelectedId < _maps.Count)
            selectedMap = _maps[_window.MapOptions.SelectedId].MapId;

        var firstUpdate = _maps.Count == 0;
        _maps.Clear();
        _maps.AddRange(maps);
        _window.MapOptions.Clear();
        foreach (var map in _maps)
        {
            var mapId = $"Map {map.MapId}";
            _window.MapOptions.AddItem(map.Name.Equals(mapId, StringComparison.OrdinalIgnoreCase)
                ? mapId
                : $"{map.Name} ({mapId})");
        }

        var mapIndex = selectedMap is { } previous
            ? _maps.FindIndex(entry => entry.MapId == previous)
            : -1;
        if (mapIndex < 0 &&
            _players.LocalEntity is { } player &&
            _entities.TryGetComponent(player, out TransformComponent? transform))
        {
            mapIndex = _maps.FindIndex(entry => entry.MapId == transform.MapID);
        }

        if (mapIndex < 0 && _maps.Count > 0)
            mapIndex = 0;

        if (mapIndex >= 0)
            _window.MapOptions.Select(mapIndex);

        UpdateCoordinateMode();

        if (firstUpdate)
            SetCurrentPosition();
    }

    private void UpdateCoordinateMode()
    {
        if (_window.MapOptions.SelectedId < 0 || _window.MapOptions.SelectedId >= _maps.Count)
            return;

        var hasPlanetCoordinates = _maps[_window.MapOptions.SelectedId].HasPlanetCoordinates;
        if (hasPlanetCoordinates)
        {
            _window.RawCoordinates.Disabled = false;
            _window.RawCoordinates.Pressed = _rawCoordinatesOnPlanet;
        }
        else
        {
            _window.RawCoordinates.Pressed = true;
            _window.RawCoordinates.Disabled = true;
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
            UpdateCoordinateMode();
        }

        var position = _transform.GetMapCoordinates(player, transform).Position.Floored();
        if (mapIndex >= 0 && !_window.RawCoordinates.Pressed)
            position += _maps[mapIndex].CoordinateOffset;

        _window.MapX.Value = position.X;
        _window.MapY.Value = position.Y;
    }

    private void ConfirmLaunch()
    {
        var total = ManifestPayloadCount();
        if (total == 0 ||
            _window.MapOptions.SelectedId < 0 ||
            _window.MapOptions.SelectedId >= _maps.Count)
        {
            _window.Result.Text = Loc.GetString("rmc-orbital-drop-result-empty");
            return;
        }

        _confirmation?.Close();
        _confirmation = new ConfirmationWindow();
        _confirmation.Setup(
            Loc.GetString("rmc-orbital-drop-confirm-title"),
            Loc.GetString("rmc-orbital-drop-confirm-text", ("entities", total), ("pods", _window.PodCount.Value)),
            Loc.GetString("rmc-orbital-drop-confirm"),
            Loc.GetString("rmc-orbital-drop-cancel"));
        _confirmation.AcceptButton.OnPressed += _ =>
        {
            _confirmation.Close();
            SendLaunch();
        };
        _confirmation.DenyButton.OnPressed += _ => _confirmation.Close();
        _confirmation.OpenCentered();
    }

    private void SendLaunch()
    {
        var map = _maps[_window.MapOptions.SelectedId];
        SendMessage(new RMCOrbitalDropLaunchMsg(
            _entityManifest.Keys.ToList(),
            _prototypeManifest.Select(entry => new RMCOrbitalDropPrototypePayload(entry.Key, entry.Value)).ToList(),
            map.MapId,
            new Vector2i(_window.MapX.Value, _window.MapY.Value),
            _window.LandingRadius.Value,
            _window.PodCount.Value,
            _window.ArrivalDelay.Value,
            _window.DropDuration.Value,
            _window.OpenDelay.Value,
            _window.LaunchInterval.Value,
            _window.DropInterval.Value,
            _window.DropIntervalVariation.Value,
            _window.UseParachute.Pressed,
            _window.RawCoordinates.Pressed,
            _window.IgnoreParadropRestrictions.Pressed));
    }

    private void Refresh(float radius)
    {
        SendMessage(new RMCOrbitalDropRefreshMsg(radius));
    }

    private void SendClosedMessage()
    {
        SendMessage(new CloseEuiMessage());
    }
}
