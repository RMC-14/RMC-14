using System.Linq;
using System.Numerics;
using Content.Server._RMC14.Dropship.Utility;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._RMC14.Admin.Utility;
using Content.Shared._RMC14.Dropship.Utility;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.SupplyDrop;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Eui;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Components;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Admin.Utility;

[AdminCommand(AdminFlags.VarEdit)]
public sealed class RMCOrbitalDropUiCommand : LocalizedCommands
{
    [Dependency] private readonly EuiManager _eui = default!;

    public override string Command => "orbitaldropui";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        _eui.OpenEui(new RMCOrbitalDropEui(), player);
    }
}

public sealed class RMCOrbitalDropEui : BaseEui
{
    private const float DefaultNearbyRadius = 7;
    private const float MaxNearbyRadius = 50;

    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IEntityManager _entities = default!;

    private readonly EntityLookupSystem _lookup;
    private readonly SharedMapSystem _map;
    private readonly RMCOrbitalDeployerSystem _orbitalDrop;
    private readonly RMCPlanetSystem _planet;
    private readonly SharedTransformSystem _transform;

    private readonly HashSet<EntityUid> _nearbyEntities = [];
    private float _nearbyRadius = DefaultNearbyRadius;

    public RMCOrbitalDropEui()
    {
        IoCManager.InjectDependencies(this);
        _lookup = _entities.System<EntityLookupSystem>();
        _map = _entities.System<SharedMapSystem>();
        _orbitalDrop = _entities.System<RMCOrbitalDeployerSystem>();
        _planet = _entities.System<RMCPlanetSystem>();
        _transform = _entities.System<SharedTransformSystem>();
    }

    public override void Opened()
    {
        _admin.OnPermsChanged += OnAdminPermsChanged;
        StateDirty();
    }

    public override void Closed()
    {
        _admin.OnPermsChanged -= OnAdminPermsChanged;
    }

    public override EuiStateBase GetNewState()
    {
        var nearby = new List<RMCOrbitalDropEntityEntry>();
        var playerControlled = new List<RMCOrbitalDropEntityEntry>();
        var source = Player.AttachedEntity;
        MapCoordinates? sourceCoordinates = null;

        if (source is { } sourceEntity &&
            !_entities.IsQueuedForDeletion(sourceEntity) &&
            _entities.TryGetComponent(sourceEntity, out MetaDataComponent? sourceMetadata) &&
            sourceMetadata.EntityLifeStage < EntityLifeStage.Terminating)
        {
            sourceCoordinates = _transform.GetMapCoordinates(sourceEntity);
            _nearbyEntities.Clear();
            _lookup.GetEntitiesInRange(
                sourceCoordinates.Value.MapId,
                sourceCoordinates.Value.Position,
                _nearbyRadius,
                _nearbyEntities,
                LookupFlags.Uncontained);
            foreach (var entity in _nearbyEntities)
            {
                if (entity == sourceEntity ||
                    (!_entities.HasComponent<MobStateComponent>(entity) &&
                     !_entities.HasComponent<CanBeSupplyDroppedComponent>(entity)) ||
                    !TryCreateEntry(entity, sourceCoordinates, out var entry))
                {
                    continue;
                }

                nearby.Add(entry);
            }
        }

        var actorQuery = _entities.EntityQueryEnumerator<ActorComponent, MetaDataComponent>();
        while (actorQuery.MoveNext(out var entity, out var actor, out _))
        {
            if (entity == source || _entities.HasComponent<GhostComponent>(entity) ||
                !TryCreateEntry(entity, sourceCoordinates, out var entry))
            {
                continue;
            }

            entry = entry with { Name = $"{entry.Name} ({actor.PlayerSession.Name})" };
            playerControlled.Add(entry);
        }

        nearby.Sort(CompareEntries);
        playerControlled.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return new RMCOrbitalDropEuiState(nearby, playerControlled, GetMaps(), _nearbyRadius);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);
        if (!HasPermission())
            return;

        switch (msg)
        {
            case RMCOrbitalDropRefreshMsg refresh:
                _nearbyRadius = Math.Clamp(refresh.NearbyRadius, 1, MaxNearbyRadius);
                StateDirty();
                break;
            case RMCOrbitalDropLaunchMsg launch:
                HandleLaunch(launch);
                break;
        }
    }

    private void HandleLaunch(RMCOrbitalDropLaunchMsg launch)
    {
        var entities = new List<EntityUid>(launch.Entities.Count);
        foreach (var netEntity in launch.Entities)
        {
            if (_entities.TryGetEntity(netEntity, out var entity))
                entities.Add(entity.Value);
        }

        MapCoordinates target;
        if (launch.RawCoordinates)
        {
            target = new MapCoordinates(launch.TargetCoordinates, launch.TargetMap);
        }
        else if (!_planet.TryPlanetToCoordinates(launch.TargetCoordinates, launch.TargetMap, out target))
        {
            SendMessage(new RMCOrbitalDropResultMsg(
                RMCOrbitalDropFailure.InvalidTarget,
                launch.PodCount,
                0));
            return;
        }

        var request = new RMCOrbitalDropRequest
        {
            Entities = entities,
            Prototypes = launch.Prototypes,
            Target = target,
            LandingRadius = launch.LandingRadius,
            PodCount = launch.PodCount,
            ArrivalDelay = launch.ArrivalDelay,
            DropDuration = launch.DropDuration,
            TimeToOpen = launch.TimeToOpen,
            LaunchInterval = launch.LaunchInterval,
            DropInterval = launch.DropInterval,
            DropIntervalVariation = launch.DropIntervalVariation,
            UseParachute = launch.UseParachute,
            IgnoreParadropRestrictions = launch.IgnoreParadropRestrictions,
        };

        var result = _orbitalDrop.TryQueueOrbitalDrop(request);
        SendMessage(new RMCOrbitalDropResultMsg(
            result.Failure,
            result.RequestedLandingTiles,
            result.ViableLandingTiles));

        _adminLog.Add(LogType.AdminCommands,
            $"{Player.Name:admin} requested an orbital drop with {entities.Count} existing entities, " +
            $"{launch.Prototypes.Sum(entry => entry.Quantity)} spawned entities, and " +
            $"{launch.PodCount} pods at {target}. Raw coordinates: {launch.RawCoordinates}; " +
            $"ignore paradrop restrictions: {launch.IgnoreParadropRestrictions}. " +
            $"Result: {result.Failure}");

        if (result.Success)
            StateDirty();
    }

    private bool TryCreateEntry(
        EntityUid entity,
        MapCoordinates? sourceCoordinates,
        out RMCOrbitalDropEntityEntry entry)
    {
        entry = default;
        if (!_entities.TryGetComponent(entity, out MetaDataComponent? metadata) ||
            metadata.EntityLifeStage >= EntityLifeStage.Terminating ||
            _entities.IsQueuedForDeletion(entity) ||
            !_entities.TryGetComponent(entity, out TransformComponent? transform) ||
            _entities.HasComponent<GhostComponent>(entity) ||
            _entities.HasComponent<MapComponent>(entity) ||
            _entities.HasComponent<MapGridComponent>(entity) ||
            _entities.HasComponent<BeingSupplyDroppedComponent>(entity) ||
            _orbitalDrop.IsPayloadReserved(entity))
        {
            return false;
        }

        var coordinates = _transform.GetMapCoordinates(entity, transform);
        var distance = sourceCoordinates is { } source && source.MapId == coordinates.MapId
            ? Vector2.Distance(source.Position, coordinates.Position)
            : -1;
        var prototype = metadata.EntityPrototype?.ID ?? string.Empty;
        entry = new RMCOrbitalDropEntityEntry(
            _entities.GetNetEntity(entity),
            metadata.EntityName,
            prototype,
            coordinates.MapId.ToString(),
            distance);
        return true;
    }

    private static int CompareEntries(RMCOrbitalDropEntityEntry left, RMCOrbitalDropEntityEntry right)
    {
        var distance = left.Distance.CompareTo(right.Distance);
        return distance != 0 ? distance : string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
    }

    private List<RMCOrbitalDropMapEntry> GetMaps()
    {
        var maps = new List<RMCOrbitalDropMapEntry>();
        var gridMaps = new HashSet<MapId>();
        var gridQuery = _entities.EntityQueryEnumerator<MapGridComponent, TransformComponent>();
        while (gridQuery.MoveNext(out _, out _, out var gridTransform))
        {
            gridMaps.Add(gridTransform.MapID);
        }

        var planets = new Dictionary<MapId, (string Name, Vector2i Offset)>();
        var planetQuery = _entities.EntityQueryEnumerator<RMCPlanetComponent, TransformComponent, MetaDataComponent>();
        while (planetQuery.MoveNext(out _, out var planet, out var xform, out var metadata))
        {
            planets.TryAdd(xform.MapID, (metadata.EntityName, planet.Offset));
        }

        foreach (var mapId in _map.GetAllMapIds())
        {
            if (mapId == MapId.Nullspace ||
                !gridMaps.Contains(mapId) ||
                !_map.TryGetMap(mapId, out var mapEntity) ||
                !_entities.TryGetComponent(mapEntity, out MetaDataComponent? metadata))
            {
                continue;
            }

            var hasPlanetCoordinates = planets.TryGetValue(mapId, out var planet);
            var name = hasPlanetCoordinates ? planet.Name : metadata.EntityName;
            if (string.IsNullOrWhiteSpace(name))
                name = $"Map {mapId}";

            maps.Add(new RMCOrbitalDropMapEntry(
                mapId,
                name,
                hasPlanetCoordinates ? planet.Offset : Vector2i.Zero,
                hasPlanetCoordinates));
        }

        maps.Sort((left, right) => ((int) left.MapId).CompareTo((int) right.MapId));
        return maps;
    }

    private void OnAdminPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player != Player)
            return;

        if (!HasPermission())
            Close();
    }

    private bool HasPermission()
    {
        return _admin.HasAdminFlag(Player, AdminFlags.VarEdit);
    }
}
