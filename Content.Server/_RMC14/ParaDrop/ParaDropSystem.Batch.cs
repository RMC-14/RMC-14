using System.Linq;
using System.Numerics;
using Content.Server._RMC14.PayloadDeployment;
using Content.Shared._RMC14.CrashLand;
using Content.Shared._RMC14.ParaDrop;
using Content.Shared._RMC14.PayloadDeployment;
using Content.Shared._RMC14.SupplyDrop;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.ParaDrop;

public sealed partial class ParaDropSystem
{
    [Dependency] private readonly SharedCrashLandSystem _crashLand = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly RMCPayloadDeploymentSystem _payloadDeployment = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly List<ParaDropJob> _batchJobs = [];
    private readonly HashSet<EntityUid> _reservedPayloads = [];

    public bool IsPayloadReserved(EntityUid entity)
    {
        return _reservedPayloads.Contains(entity);
    }

    public RMCPayloadDeploymentResult TryQueueParaDropBatch(IReadOnlyList<RMCParaDropRequest> requests)
    {
        if (requests.Count is <= 0 or > RMCPayloadDeploymentLimits.MaxBatchRequests)
            return new RMCPayloadDeploymentResult(RMCPayloadDeploymentFailure.InvalidSettings);

        var prepared = new List<PreparedParaDrop>(requests.Count);
        var batchPayload = new HashSet<EntityUid>();
        var assignedTiles = new HashSet<ReservedTile>();
        var spawnedPayload = new List<EntityUid>();
        var totalPayload = 0;

        for (var requestIndex = 0; requestIndex < requests.Count; requestIndex++)
        {
            var request = requests[requestIndex];
            if (!ValidateParaDropRequest(request, out var existingPayload, out var requestPayload))
            {
                CleanupEntities(spawnedPayload);
                return new RMCPayloadDeploymentResult(RMCPayloadDeploymentFailure.InvalidSettings, requestIndex);
            }

            foreach (var entity in existingPayload)
            {
                if (batchPayload.Add(entity))
                    continue;

                CleanupEntities(spawnedPayload);
                return new RMCPayloadDeploymentResult(RMCPayloadDeploymentFailure.InvalidPayload, requestIndex);
            }

            if (totalPayload + requestPayload > RMCPayloadDeploymentLimits.MaxPayload)
            {
                CleanupEntities(spawnedPayload);
                return new RMCPayloadDeploymentResult(RMCPayloadDeploymentFailure.InvalidSettings, requestIndex);
            }

            totalPayload += requestPayload;
            var stagingOrigin = _payloadDeployment.AllocateStagingGroup();
            var payload = new List<EntityUid>(requestPayload);
            payload.AddRange(existingPayload);
            foreach (var prototypePayload in request.Prototypes)
            {
                if (!_prototypes.TryIndex(prototypePayload.Prototype, out var prototype) ||
                    prototype.Abstract)
                {
                    CleanupEntities(spawnedPayload);
                    return new RMCPayloadDeploymentResult(RMCPayloadDeploymentFailure.InvalidPrototype, requestIndex);
                }

                for (var i = 0; i < prototypePayload.Quantity; i++)
                {
                    var spawned = Spawn(prototypePayload.Prototype, _payloadDeployment.GetStagingCoordinates(stagingOrigin));
                    spawnedPayload.Add(spawned);
                    payload.Add(spawned);
                }
            }

            if (!_mapManager.TryFindGridAt(request.Target, out var grid, out var gridComponent))
            {
                CleanupEntities(spawnedPayload);
                return new RMCPayloadDeploymentResult(
                    RMCPayloadDeploymentFailure.InvalidTarget,
                    requestIndex,
                    requestPayload);
            }

            if (!TryFindLandingTiles(request, (grid, gridComponent), assignedTiles, payload,
                    out var landingTiles, out var assignedLandings))
            {
                CleanupEntities(spawnedPayload);
                return new RMCPayloadDeploymentResult(
                    RMCPayloadDeploymentFailure.InsufficientLandingTiles,
                    requestIndex,
                    requestPayload,
                    assignedLandings);
            }

            var queued = new List<QueuedParaDrop>(payload.Count);
            var existingPayloadSet = existingPayload.ToHashSet();
            var previousArrivalAt = TimeSpan.Zero;
            for (var i = 0; i < payload.Count; i++)
            {
                var launchAt = _timing.CurTime + TimeSpan.FromSeconds(request.LaunchInterval * i);
                var arrivalAt = launchAt + TimeSpan.FromSeconds(request.ArrivalDelay);
                if (i > 0)
                {
                    var minimum = Math.Max(0, request.ArrivalInterval - request.ArrivalIntervalVariation);
                    var maximum = request.ArrivalInterval + request.ArrivalIntervalVariation;
                    var spacedArrivalAt = previousArrivalAt +
                        TimeSpan.FromSeconds(_random.NextFloat(minimum, maximum));
                    if (spacedArrivalAt > arrivalAt)
                        arrivalAt = spacedArrivalAt;
                }

                previousArrivalAt = arrivalAt;
                var landing = landingTiles[i];
                var stagingOffset = landing.Coordinates.Position - request.Target.Position;
                var staging = _payloadDeployment.GetStagingCoordinates(stagingOrigin + stagingOffset);
                if (!existingPayloadSet.Contains(payload[i]))
                    _transform.SetMapCoordinates(payload[i], staging);

                queued.Add(new QueuedParaDrop(
                    payload[i],
                    existingPayloadSet.Contains(payload[i]),
                    landing.Coordinates,
                    staging,
                    launchAt,
                    arrivalAt));
            }

            prepared.Add(new PreparedParaDrop(request, existingPayload, queued));
        }

        foreach (var drop in prepared)
        {
            foreach (var entity in drop.ExistingPayload)
            {
                _reservedPayloads.Add(entity);
            }

            _batchJobs.Add(new ParaDropJob(drop.Request, drop.Entities));
        }

        return new RMCPayloadDeploymentResult(RMCPayloadDeploymentFailure.None);
    }

    private bool ValidateParaDropRequest(
        RMCParaDropRequest request,
        out List<EntityUid> existingPayload,
        out int totalPayload)
    {
        existingPayload = request.Entities.Distinct().ToList();
        totalPayload = existingPayload.Count;

        if (!IsValidTiming(request.ArrivalDelay) ||
            !IsValidTiming(request.DropDuration) ||
            !IsValidTiming(request.LaunchInterval) ||
            !IsValidTiming(request.ArrivalInterval) ||
            !IsValidTiming(request.ArrivalIntervalVariation) ||
            !float.IsFinite(request.Target.X) ||
            !float.IsFinite(request.Target.Y) ||
            request.LandingRadius < 0 ||
            request.LandingRadius > RMCPayloadDeploymentLimits.MaxLandingRadius)
        {
            return false;
        }

        foreach (var entity in existingPayload)
        {
            if (TerminatingOrDeleted(entity) ||
                EntityManager.IsQueuedForDeletion(entity) ||
                _reservedPayloads.Contains(entity) ||
                HasComp<GhostComponent>(entity) ||
                HasComp<SkyFallingComponent>(entity) ||
                HasComp<ParaDroppingComponent>(entity) ||
                HasComp<BeingSupplyDroppedComponent>(entity) ||
                (!HasComp<MobStateComponent>(entity) &&
                 !HasComp<CanBeSupplyDroppedComponent>(entity) &&
                 !HasComp<ParaDroppableComponent>(entity)))
            {
                return false;
            }
        }

        foreach (var prototype in request.Prototypes)
        {
            if (prototype.Quantity <= 0 ||
                prototype.Quantity > RMCPayloadDeploymentLimits.MaxPayload - totalPayload)
            {
                return false;
            }

            totalPayload += prototype.Quantity;
        }

        return totalPayload > 0 && totalPayload <= RMCPayloadDeploymentLimits.MaxPayload;
    }

    private bool TryFindLandingTiles(RMCParaDropRequest request, Entity<MapGridComponent> grid, HashSet<ReservedTile> assignedTiles, IReadOnlyList<EntityUid> payload, out List<LandingTile> selected, out int assignedCount)
    {
        selected = [];
        assignedCount = 0;
        var centerCoordinates = _transform.ToCoordinates(request.Target);
        var center = _map.LocalToTile(grid, grid.Comp, centerCoordinates);
        var centerPosition = _map.GridTileToLocal(grid, grid.Comp, center).Position;
        var candidates = new List<Vector2i>();
        foreach (var tileRef in _map.GetLocalTilesIntersecting(
                     grid,
                     grid.Comp,
                     new Circle(centerPosition, request.LandingRadius * grid.Comp.TileSize),
                     false))
        {
            candidates.Add(tileRef.GridIndices);
        }

        _random.Shuffle(candidates);
        foreach (var entity in payload)
        {
            LandingTile? fallback = null;
            LandingTile? landing = null;
            foreach (var candidate in candidates)
            {
                if (!_crashLand.TryGetLandableFootprint(entity,
                        grid,
                        candidate,
                        request.IgnoreParadropRestrictions,
                        out var footprint))
                {
                    continue;
                }

                var coordinates = _transform.ToMapCoordinates(_map.GridTileToLocal(grid, grid.Comp, candidate));
                var tiles = new List<ReservedTile>(footprint.Count);
                var overlapsAssignedTile = false;
                foreach (var tileRef in footprint)
                {
                    var tile = new ReservedTile(grid.Owner, tileRef.GridIndices);
                    tiles.Add(tile);
                    if (assignedTiles.Contains(tile))
                        overlapsAssignedTile = true;
                }

                var candidateLanding = new LandingTile(coordinates, tiles);
                fallback ??= candidateLanding;
                if (overlapsAssignedTile)
                    continue;

                landing = candidateLanding;
                break;
            }

            landing ??= fallback;
            if (landing == null)
                return false;

            selected.Add(landing.Value);
            assignedCount = selected.Count;
            foreach (var tile in landing.Value.Tiles)
            {
                assignedTiles.Add(tile);
            }
        }

        return true;
    }

    private static bool IsValidTiming(float value)
    {
        return value is >= 0 and <= RMCPayloadDeploymentLimits.MaxTimingSeconds;
    }

    private void CleanupEntities(IEnumerable<EntityUid> entities)
    {
        foreach (var entity in entities)
        {
            if (TerminatingOrDeleted(entity) || EntityManager.IsQueuedForDeletion(entity))
                continue;

            QueueDel(entity);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        for (var jobIndex = _batchJobs.Count - 1; jobIndex >= 0; jobIndex--)
        {
            var job = _batchJobs[jobIndex];
            while (job.NextEntity < job.Entities.Count && curTime >= job.Entities[job.NextEntity].LaunchAt)
            {
                var queued = job.Entities[job.NextEntity++];
                if (queued.Existing)
                    _reservedPayloads.Remove(queued.Entity);

                if (TerminatingOrDeleted(queued.Entity) ||
                    EntityManager.IsQueuedForDeletion(queued.Entity) ||
                    HasComp<SkyFallingComponent>(queued.Entity) ||
                    HasComp<ParaDroppingComponent>(queued.Entity) ||
                    HasComp<BeingSupplyDroppedComponent>(queued.Entity))
                {
                    if (!queued.Existing &&
                        !TerminatingOrDeleted(queued.Entity) &&
                        !EntityManager.IsQueuedForDeletion(queued.Entity))
                    {
                        QueueDel(queued.Entity);
                    }

                    continue;
                }

                _transform.SetMapCoordinates(queued.Entity, queued.StagingCoordinates);
                var skyFallDuration = Math.Max(0, (float) (queued.ArrivalAt - curTime).TotalSeconds);
                StartPreparedParaDrop(
                    queued.Entity,
                    _transform.ToCoordinates(queued.LandingCoordinates),
                    skyFallDuration,
                    job.Request.DropDuration);
            }

            if (job.NextEntity >= job.Entities.Count)
                _batchJobs.RemoveAt(jobIndex);
        }
    }

    private readonly record struct ReservedTile(EntityUid Grid, Vector2i Indices);
    private readonly record struct LandingTile(MapCoordinates Coordinates, List<ReservedTile> Tiles);
    private readonly record struct QueuedParaDrop(
        EntityUid Entity,
        bool Existing,
        MapCoordinates LandingCoordinates,
        MapCoordinates StagingCoordinates,
        TimeSpan LaunchAt,
        TimeSpan ArrivalAt);

    private sealed class PreparedParaDrop(
        RMCParaDropRequest request,
        List<EntityUid> existingPayload,
        List<QueuedParaDrop> entities)
    {
        public readonly RMCParaDropRequest Request = request;
        public readonly List<EntityUid> ExistingPayload = existingPayload;
        public readonly List<QueuedParaDrop> Entities = entities;
    }

    private sealed class ParaDropJob(RMCParaDropRequest request, List<QueuedParaDrop> entities)
    {
        public readonly RMCParaDropRequest Request = request;
        public readonly List<QueuedParaDrop> Entities = entities;
        public int NextEntity;
    }
}
