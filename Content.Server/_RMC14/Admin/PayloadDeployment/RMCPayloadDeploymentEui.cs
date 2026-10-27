using System.Linq;
using System.Numerics;
using Content.Server._RMC14.ParaDrop;
using Content.Server._RMC14.PayloadDeployment;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._RMC14.Admin.PayloadDeployment;
using Content.Shared._RMC14.ParaDrop;
using Content.Shared._RMC14.PayloadDeployment;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.SupplyDrop;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Eui;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Admin.PayloadDeployment;

public sealed class RMCPayloadDeploymentEui : BaseEui
{
    private const int DefaultNearbyRadius = 7;
    private const int MaxNearbyRadius = 50;

    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IEntityManager _entities = default!;

    private readonly EntityLookupSystem _lookup;
    private readonly SharedMapSystem _map;
    private readonly SharedMindSystem _mind;
    private readonly RMCOrbitalDeploySystem _orbitalDrop;
    private readonly ParaDropSystem _paraDrop;
    private readonly RMCPlanetSystem _planet;
    private readonly SharedJobSystem _job;
    private readonly SharedTransformSystem _transform;

    private readonly HashSet<EntityUid> _nearbyEntities = [];
    private int _nearbyRadius = DefaultNearbyRadius;

    public RMCPayloadDeploymentEui()
    {
        IoCManager.InjectDependencies(this);
        _lookup = _entities.System<EntityLookupSystem>();
        _map = _entities.System<SharedMapSystem>();
        _mind = _entities.System<SharedMindSystem>();
        _orbitalDrop = _entities.System<RMCOrbitalDeploySystem>();
        _paraDrop = _entities.System<ParaDropSystem>();
        _planet = _entities.System<RMCPlanetSystem>();
        _job = _entities.System<SharedJobSystem>();
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
        var nearby = new List<RMCPayloadDeploymentEntityEntry>();
        var playerControlled = new List<RMCPayloadDeploymentEntityEntry>();
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
                if ((!_entities.HasComponent<MobStateComponent>(entity) &&
                     !_entities.HasComponent<CanBeSupplyDroppedComponent>(entity) &&
                     !_entities.HasComponent<ParaDroppableComponent>(entity)) ||
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
            if (!TryCreateEntry(entity, sourceCoordinates, out var entry))
            {
                continue;
            }

            entry = entry with { Name = $"{entry.Name} ({actor.PlayerSession.Name})" };
            playerControlled.Add(entry);
        }

        nearby.Sort(CompareEntries);
        playerControlled.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return new RMCPayloadDeploymentEuiState(nearby, playerControlled, GetMaps(), _nearbyRadius);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);
        if (!HasPermission())
            return;

        switch (msg)
        {
            case RMCPayloadDeploymentRefreshMsg refresh:
                _nearbyRadius = Math.Clamp(refresh.NearbyRadius, 1, MaxNearbyRadius);
                StateDirty();
                break;
            case RMCPayloadDeploymentValidateEntitiesMsg validate:
                ValidateEntities(validate.Entities);
                break;
            case RMCOrbitalDropBatchLaunchMsg orbital:
                HandleOrbitalLaunch(orbital);
                break;
            case RMCParaDropBatchLaunchMsg paraDrop:
                HandleParaDropLaunch(paraDrop);
                break;
        }
    }

    private void ValidateEntities(IEnumerable<NetEntity> entities)
    {
        var invalid = new List<NetEntity>();
        foreach (var netEntity in entities)
        {
            if (_entities.TryGetEntity(netEntity, out _))
                continue;

            invalid.Add(netEntity);
        }

        if (invalid.Count > 0)
            SendMessage(new RMCPayloadDeploymentInvalidEntitiesMsg(invalid));
    }

    private void HandleOrbitalLaunch(RMCOrbitalDropBatchLaunchMsg launch)
    {
        var requests = new List<RMCOrbitalDropRequest>(launch.Manifests.Count);
        for (var manifest = 0; manifest < launch.Manifests.Count; manifest++)
        {
            var entry = launch.Manifests[manifest];
            if (!TryGetTarget(entry.TargetMap, entry.TargetCoordinates, entry.RawCoordinates, out var target))
            {
                SendResult(new RMCPayloadDeploymentResult(RMCPayloadDeploymentFailure.InvalidTarget, manifest, entry.PodCount));
                return;
            }

            if (!TryResolveEntities(entry.Entities, out var entities))
            {
                SendResult(new RMCPayloadDeploymentResult(RMCPayloadDeploymentFailure.InvalidPayload, manifest));
                return;
            }

            requests.Add(new RMCOrbitalDropRequest
            {
                Entities = entities,
                Prototypes = entry.Prototypes,
                Target = target,
                LandingRadius = entry.LandingRadius,
                UseDropPods = entry.UseDropPods,
                PodCount = entry.PodCount,
                ArrivalDelay = entry.ArrivalDelay,
                DropDuration = entry.DropDuration,
                TimeToOpen = entry.TimeToOpen,
                LaunchInterval = entry.LaunchInterval,
                DropInterval = entry.DropInterval,
                DropIntervalVariation = entry.DropIntervalVariation,
                UseParachute = entry.UseParachute,
                ShowLandingWarning = entry.ShowLandingWarning,
                IgnoreParadropRestrictions = entry.IgnoreParadropRestrictions,
            });
        }

        var result = _orbitalDrop.TryQueueOrbitalDropBatch(requests);
        SendResult(result);
        var deliveries = requests.Sum(request => request.UseDropPods
            ? request.PodCount
            : request.Entities.Count + request.Prototypes.Sum(entry => entry.Quantity));
        _adminLog.Add(LogType.AdminCommands,
            $"{Player.Name:admin} requested {requests.Count} orbital drop manifests with " +
            $"{requests.Sum(request => request.Entities.Count)} existing entities, " +
            $"{requests.Sum(request => request.Prototypes.Sum(entry => entry.Quantity))} spawned entities, and " +
            $"{deliveries} deliveries. Result: {result.Failure}; " +
            $"failed manifest: {result.FailedRequest}.");

        if (result.Success)
            StateDirty();
    }

    private void HandleParaDropLaunch(RMCParaDropBatchLaunchMsg launch)
    {
        var requests = new List<RMCParaDropRequest>(launch.Manifests.Count);
        for (var manifest = 0; manifest < launch.Manifests.Count; manifest++)
        {
            var entry = launch.Manifests[manifest];
            if (!TryGetTarget(entry.TargetMap, entry.TargetCoordinates, entry.RawCoordinates, out var target))
            {
                SendResult(new RMCPayloadDeploymentResult(RMCPayloadDeploymentFailure.InvalidTarget, manifest));
                return;
            }

            if (!TryResolveEntities(entry.Entities, out var entities))
            {
                SendResult(new RMCPayloadDeploymentResult(RMCPayloadDeploymentFailure.InvalidPayload, manifest));
                return;
            }

            requests.Add(new RMCParaDropRequest
            {
                Entities = entities,
                Prototypes = entry.Prototypes,
                Target = target,
                LandingRadius = entry.LandingRadius,
                ArrivalDelay = entry.ArrivalDelay,
                DropDuration = entry.DropDuration,
                LaunchInterval = entry.LaunchInterval,
                ArrivalInterval = entry.ArrivalInterval,
                ArrivalIntervalVariation = entry.ArrivalIntervalVariation,
                IgnoreParadropRestrictions = entry.IgnoreParadropRestrictions,
            });
        }

        var result = _paraDrop.TryQueueParaDropBatch(requests);
        SendResult(result);
        _adminLog.Add(LogType.AdminCommands,
            $"{Player.Name:admin} requested {requests.Count} paradrop manifests with " +
            $"{requests.Sum(request => request.Entities.Count)} existing entities and " +
            $"{requests.Sum(request => request.Prototypes.Sum(entry => entry.Quantity))} spawned entities. " +
            $"Result: {result.Failure}; failed manifest: {result.FailedRequest}.");

        if (result.Success)
            StateDirty();
    }

    private bool TryGetTarget(MapId map, Vector2i coordinates, bool rawCoordinates, out MapCoordinates target)
    {
        if (rawCoordinates)
        {
            target = new MapCoordinates(coordinates, map);
            return true;
        }

        return _planet.TryPlanetToCoordinates(coordinates, map, out target);
    }

    private bool TryResolveEntities(IEnumerable<NetEntity> entities, out List<EntityUid> resolved)
    {
        resolved = [];
        foreach (var netEntity in entities)
        {
            if (!_entities.TryGetEntity(netEntity, out var entity))
                return false;

            resolved.Add(entity.Value);
        }

        return true;
    }

    private void SendResult(RMCPayloadDeploymentResult result)
    {
        SendMessage(new RMCPayloadDeploymentResultMsg(
            result.Failure,
            result.FailedRequest,
            result.RequestedLandings,
            result.AssignedLandings));
    }

    private bool TryCreateEntry(EntityUid entity, MapCoordinates? sourceCoordinates, out RMCPayloadDeploymentEntityEntry entry)
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
            _orbitalDrop.IsPayloadReserved(entity) ||
            _paraDrop.IsPayloadReserved(entity))
        {
            return false;
        }

        var coordinates = _transform.GetMapCoordinates(entity, transform);
        var distance = sourceCoordinates is { } source && source.MapId == coordinates.MapId
            ? Vector2.Distance(source.Position, coordinates.Position)
            : -1;
        var prototype = metadata.EntityPrototype?.ID ?? string.Empty;
        var role = string.Empty;
        if (_entities.HasComponent<ActorComponent>(entity) &&
            _mind.TryGetMind(entity, out var mindId, out _) &&
            _job.MindTryGetJobName(mindId, out var jobName))
        {
            role = jobName;
        }

        entry = new RMCPayloadDeploymentEntityEntry(
            _entities.GetNetEntity(entity),
            metadata.EntityName,
            prototype,
            coordinates.MapId,
            role,
            distance);
        return true;
    }

    private static int CompareEntries(RMCPayloadDeploymentEntityEntry left, RMCPayloadDeploymentEntityEntry right)
    {
        var distance = left.Distance.CompareTo(right.Distance);
        return distance != 0 ? distance : string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
    }

    private List<RMCPayloadDeploymentMapEntry> GetMaps()
    {
        var maps = new List<RMCPayloadDeploymentMapEntry>();
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

            maps.Add(new RMCPayloadDeploymentMapEntry(
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
