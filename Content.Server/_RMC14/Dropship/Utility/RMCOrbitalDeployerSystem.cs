using System.Linq;
using Content.Shared._RMC14.Dropship.Utility;
using Content.Shared._RMC14.CrashLand;
using Content.Shared._RMC14.Dropship.Utility.Systems;
using Content.Shared._RMC14.ParaDrop;
using Content.Shared._RMC14.SupplyDrop;
using Content.Shared.GameTicking;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Dropship.Utility;

public sealed class RMCOrbitalDeployerSystem : SharedRMCOrbitalDeployerSystem
{
    [Dependency] private readonly SharedCrashLandSystem _crashLand = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly List<OrbitalDropJob> _jobs = [];
    private readonly HashSet<EntityUid> _reservedPayloads = [];
    private readonly Dictionary<ReservedTile, TimeSpan> _reservedTiles = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<SupplyDropPodComponent, ParaDropFinishedEvent>(OnParaDropFinished);
        SubscribeLocalEvent<SupplyDropPodComponent, CrashLandedEvent>(OnParaDropFinished);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _jobs.Clear();
        _reservedPayloads.Clear();
        _reservedTiles.Clear();
    }

    private void OnParaDropFinished<T>(Entity<SupplyDropPodComponent> ent, ref T args)
    {
        ent.Comp.Landed = true;
        Dirty(ent);
    }

    public bool IsPayloadReserved(EntityUid entity)
    {
        return _reservedPayloads.Contains(entity);
    }

    public RMCPayloadDeploymentResult TryQueueOrbitalDrop(RMCOrbitalDropRequest request)
    {
        return TryQueueOrbitalDropBatch([request]);
    }

    public RMCPayloadDeploymentResult TryQueueOrbitalDropBatch(IReadOnlyList<RMCOrbitalDropRequest> requests)
    {
        if (requests.Count is <= 0 or > RMCPayloadDeploymentLimits.MaxBatchRequests)
            return new RMCPayloadDeploymentResult(RMCPayloadDeploymentFailure.InvalidSettings);

        var prepared = new List<PreparedOrbitalDrop>(requests.Count);
        var batchPayload = new HashSet<EntityUid>();
        var batchTiles = new HashSet<ReservedTile>();
        var totalPayload = 0;
        var totalPods = 0;

        for (var requestIndex = 0; requestIndex < requests.Count; requestIndex++)
        {
            var request = requests[requestIndex];
            if (!ValidateRequest(request, out var existingPayload, out var requestPayload))
            {
                CleanupPreparedDrops(prepared);
                return new RMCPayloadDeploymentResult(RMCPayloadDeploymentFailure.InvalidSettings, requestIndex);
            }

            if (existingPayload.Any(entity => !batchPayload.Add(entity)))
            {
                CleanupPreparedDrops(prepared);
                return new RMCPayloadDeploymentResult(RMCPayloadDeploymentFailure.InvalidPayload, requestIndex);
            }

            if (totalPayload + requestPayload > RMCPayloadDeploymentLimits.MaxPayload ||
                totalPods + request.PodCount > RMCPayloadDeploymentLimits.MaxPods)
            {
                CleanupPreparedDrops(prepared);
                return new RMCPayloadDeploymentResult(RMCPayloadDeploymentFailure.InvalidSettings, requestIndex);
            }

            totalPayload += requestPayload;
            totalPods += request.PodCount;
            if (!TryPrepareOrbitalDrop(request,
                    existingPayload,
                    requestPayload,
                    batchTiles,
                    out var drop,
                    out var failure,
                    out var viableTiles))
            {
                CleanupPreparedDrops(prepared);
                return new RMCPayloadDeploymentResult(
                    failure,
                    requestIndex,
                    request.PodCount,
                    Math.Max(0, viableTiles));
            }

            prepared.Add(drop);
            foreach (var landing in drop.LandingTiles)
            {
                batchTiles.Add(landing.Tile);
            }
        }

        foreach (var drop in prepared)
        {
            CommitPreparedDrop(drop);
        }

        return new RMCPayloadDeploymentResult(RMCPayloadDeploymentFailure.None);
    }

    private bool TryPrepareOrbitalDrop(
        RMCOrbitalDropRequest request,
        List<EntityUid> existingPayload,
        int totalPayload,
        HashSet<ReservedTile> batchTiles,
        out PreparedOrbitalDrop prepared,
        out RMCPayloadDeploymentFailure failure,
        out int viableTiles)
    {
        prepared = default!;
        failure = RMCPayloadDeploymentFailure.None;
        if (!TryFindLandingTiles(request, batchTiles, out var landingTiles, out viableTiles))
        {
            failure = viableTiles < 0
                ? RMCPayloadDeploymentFailure.InvalidTarget
                : RMCPayloadDeploymentFailure.InsufficientLandingTiles;
            return false;
        }

        var spawnedPayload = new List<EntityUid>();
        foreach (var prototypePayload in request.Prototypes)
        {
            if (!_prototypes.TryIndex(prototypePayload.Prototype, out EntityPrototype? prototype) ||
                prototype.Abstract)
            {
                CleanupEntities(spawnedPayload);
                failure = RMCPayloadDeploymentFailure.InvalidPrototype;
                return false;
            }

            for (var i = 0; i < prototypePayload.Quantity; i++)
            {
                spawnedPayload.Add(Spawn(prototypePayload.Prototype));
            }
        }

        var payload = new List<EntityUid>(totalPayload);
        payload.AddRange(existingPayload);
        payload.AddRange(spawnedPayload);

        var podPayloads = new List<List<EntityUid>>(request.PodCount);
        for (var i = 0; i < request.PodCount; i++)
        {
            podPayloads.Add([]);
        }

        for (var i = 0; i < payload.Count; i++)
        {
            podPayloads[i % request.PodCount].Add(payload[i]);
        }

        var preparedPods = new List<EntityUid>(request.PodCount);
        var podContainers = new List<BaseContainer>(request.PodCount);
        for (var i = 0; i < request.PodCount; i++)
        {
            if (!TryCreateOrbitalDropPod(request.TimeToOpen, out var pod) ||
                !TryComp(pod, out SupplyDropPodComponent? podComponent))
            {
                CleanupEntities(preparedPods);
                CleanupEntities(spawnedPayload);
                failure = RMCPayloadDeploymentFailure.PodPreparationFailed;
                return false;
            }

            preparedPods.Add(pod);
            podContainers.Add(Container.EnsureContainer<Container>(pod, podComponent.DeploySlotId));
        }

        for (var i = 0; i < podPayloads.Count; i++)
        {
            foreach (var entity in podPayloads[i])
            {
                if (!TerminatingOrDeleted(entity) &&
                    !EntityManager.IsQueuedForDeletion(entity) &&
                    Container.CanInsert(entity, podContainers[i]))
                    continue;

                CleanupEntities(preparedPods);
                CleanupEntities(spawnedPayload);
                failure = RMCPayloadDeploymentFailure.InvalidPayload;
                return false;
            }
        }

        var existingPayloadSet = existingPayload.ToHashSet();
        for (var i = 0; i < podPayloads.Count; i++)
        {
            foreach (var entity in podPayloads[i])
            {
                if (existingPayloadSet.Contains(entity))
                    continue;

                if (Container.Insert(entity, podContainers[i]))
                    continue;

                CleanupPreparedPayload(preparedPods, spawnedPayload);
                failure = RMCPayloadDeploymentFailure.PodPreparationFailed;
                return false;
            }
        }

        prepared = new PreparedOrbitalDrop(
            request,
            existingPayload,
            spawnedPayload,
            podPayloads,
            preparedPods,
            landingTiles);
        return true;
    }

    private void CommitPreparedDrop(PreparedOrbitalDrop prepared)
    {
        foreach (var entity in prepared.ExistingPayload)
        {
            _reservedPayloads.Add(entity);
        }

        var existingPayloadSet = prepared.ExistingPayload.ToHashSet();
        var previousDropAt = TimeSpan.Zero;
        var queuedPods = new List<QueuedDropPod>(prepared.Request.PodCount);
        for (var i = 0; i < prepared.PreparedPods.Count; i++)
        {
            var launchAt = _timing.CurTime + TimeSpan.FromSeconds(prepared.Request.LaunchInterval * i);
            var dropAt = launchAt + TimeSpan.FromSeconds(prepared.Request.ArrivalDelay);
            if (i > 0)
            {
                var minimum = Math.Max(0, prepared.Request.DropInterval - prepared.Request.DropIntervalVariation);
                var maximum = prepared.Request.DropInterval + prepared.Request.DropIntervalVariation;
                var spacedDropAt = previousDropAt + TimeSpan.FromSeconds(_random.NextFloat(minimum, maximum));
                if (spacedDropAt > dropAt)
                    dropAt = spacedDropAt;
            }

            previousDropAt = dropAt;
            var landing = prepared.LandingTiles[i];
            var pendingEntities = prepared.PodPayloads[i].Where(existingPayloadSet.Contains).ToList();
            var reservationUntil = dropAt +
                TimeSpan.FromSeconds(prepared.Request.DropDuration + prepared.Request.TimeToOpen);
            _reservedTiles[landing.Tile] = reservationUntil;
            queuedPods.Add(new QueuedDropPod(
                prepared.PreparedPods[i],
                landing.Coordinates,
                landing.Tile,
                pendingEntities,
                [],
                launchAt,
                dropAt));
        }

        _jobs.Add(new OrbitalDropJob(queuedPods, prepared.Request));
    }

    private bool ValidateRequest(RMCOrbitalDropRequest request, out List<EntityUid> existingPayload, out int totalPayload)
    {
        existingPayload = request.Entities.Distinct().ToList();
        totalPayload = existingPayload.Count;

        if (!IsValidTiming(request.ArrivalDelay) ||
            !IsValidTiming(request.DropDuration) ||
            !IsValidTiming(request.TimeToOpen) ||
            !IsValidTiming(request.LaunchInterval) ||
            !IsValidTiming(request.DropInterval) ||
            !IsValidTiming(request.DropIntervalVariation) ||
            !float.IsFinite(request.Target.X) || !float.IsFinite(request.Target.Y) ||
            request.LandingRadius < 0 || request.LandingRadius > RMCPayloadDeploymentLimits.MaxLandingRadius ||
            request.PodCount <= 0 || request.PodCount > RMCPayloadDeploymentLimits.MaxPods)
        {
            return false;
        }

        foreach (var entity in existingPayload)
        {
            if (TerminatingOrDeleted(entity) ||
                EntityManager.IsQueuedForDeletion(entity) ||
                _reservedPayloads.Contains(entity) ||
                IsBeingSupplyDropped(entity))
            {
                return false;
            }
        }

        foreach (var prototype in request.Prototypes)
        {
            if (prototype.Quantity <= 0 || prototype.Quantity > RMCPayloadDeploymentLimits.MaxPayload - totalPayload)
                return false;

            totalPayload += prototype.Quantity;
        }

        return totalPayload > 0 &&
               totalPayload <= RMCPayloadDeploymentLimits.MaxPayload &&
               request.PodCount <= totalPayload;
    }

    private static bool IsValidTiming(float value)
    {
        return value is >= 0 and <= RMCPayloadDeploymentLimits.MaxTimingSeconds;
    }

    private bool TryFindLandingTiles(
        RMCOrbitalDropRequest request,
        HashSet<ReservedTile> batchTiles,
        out List<LandingTile> selected,
        out int viableCount)
    {
        selected = [];
        viableCount = -1;
        if (!_mapManager.TryFindGridAt(request.Target, out var grid, out var gridComponent))
            return false;

        var centerCoordinates = _transform.ToCoordinates(request.Target);
        var center = _map.LocalToTile(grid, gridComponent, centerCoordinates);
        var centerPosition = _map.GridTileToLocal(grid, gridComponent, center).Position;
        var candidates = new List<LandingTile>();
        foreach (var tileRef in _map.GetLocalTilesIntersecting(
                     grid,
                     gridComponent,
                     new Circle(centerPosition, request.LandingRadius * gridComponent.TileSize),
                     false))
        {
            var key = new ReservedTile(grid, tileRef.GridIndices);
            if (batchTiles.Contains(key) ||
                _reservedTiles.TryGetValue(key, out var reservedUntil) && reservedUntil > _timing.CurTime)
                continue;

            if (!_crashLand.IsLandableTile((grid, gridComponent), tileRef, request.IgnoreParadropRestrictions))
                continue;

            var coordinates = _transform.ToMapCoordinates(_map.GridTileToLocal(grid, gridComponent, tileRef.GridIndices));
            candidates.Add(new LandingTile(coordinates, key));
        }

        viableCount = candidates.Count;
        if (viableCount < request.PodCount)
            return false;

        _random.Shuffle(candidates);
        selected.AddRange(candidates.Take(request.PodCount));
        return true;
    }

    private void CleanupPreparedPayload(List<EntityUid> preparedPods, List<EntityUid> spawnedPayload)
    {
        foreach (var pod in preparedPods)
        {
            if (TryComp(pod, out SupplyDropPodComponent? component) &&
                Container.TryGetContainer(pod, component.DeploySlotId, out var container))
            {
                Container.EmptyContainer(container, true);
            }
        }

        CleanupEntities(preparedPods);
        CleanupEntities(spawnedPayload);
    }

    private void CleanupPreparedDrops(IEnumerable<PreparedOrbitalDrop> prepared)
    {
        foreach (var drop in prepared)
        {
            CleanupPreparedPayload(drop.PreparedPods, drop.SpawnedPayload);
        }
    }

    private void LaunchQueuedPod(QueuedDropPod queued, RMCOrbitalDropRequest request)
    {
        if (TerminatingOrDeleted(queued.Pod) || EntityManager.IsQueuedForDeletion(queued.Pod))
        {
            ReleasePayloadReservations(queued.PendingEntities);
            _reservedTiles.Remove(queued.Tile);
            return;
        }

        if (!TryComp(queued.Pod, out SupplyDropPodComponent? podComponent) ||
            !Container.TryGetContainer(queued.Pod, podComponent.DeploySlotId, out var podContainer))
        {
            ReleasePayloadReservations(queued.PendingEntities);
            _reservedTiles.Remove(queued.Tile);
            QueueDel(queued.Pod);
            return;
        }

        var launchCoordinates = new List<EntityCoordinates>(queued.LaunchCoordinates);
        foreach (var entity in queued.PendingEntities)
        {
            _reservedPayloads.Remove(entity);
            if (TerminatingOrDeleted(entity) ||
                EntityManager.IsQueuedForDeletion(entity) ||
                IsBeingSupplyDropped(entity) ||
                !Container.CanInsert(entity, podContainer))
            {
                continue;
            }

            var origin = _transform.GetMoverCoordinates(entity);
            if (!Container.Insert(entity, podContainer))
                continue;

            launchCoordinates.Add(origin);
        }

        if (podContainer.ContainedEntities.Count == 0)
        {
            QueueDel(queued.Pod);
            _reservedTiles.Remove(queued.Tile);
            return;
        }

        var timeUntilDrop = Math.Max(0, (float) (queued.DropAt - _timing.CurTime).TotalSeconds);
        _reservedTiles[queued.Tile] = _timing.CurTime +
            TimeSpan.FromSeconds(timeUntilDrop + request.DropDuration + request.TimeToOpen);
        LaunchOrbitalDropPod(
            queued.Pod,
            queued.Coordinates,
            timeUntilDrop,
            request.DropDuration,
            request.UseParachute,
            launchCoordinates);
    }

    private bool IsBeingSupplyDropped(EntityUid entity)
    {
        if (HasComp<BeingSupplyDroppedComponent>(entity))
            return true;

        return Container.TryGetOuterContainer(entity, Transform(entity), out var outerContainer) &&
               HasComp<BeingSupplyDroppedComponent>(outerContainer.Owner);
    }

    private void ReleasePayloadReservations(IEnumerable<EntityUid> entities)
    {
        foreach (var entity in entities)
        {
            _reservedPayloads.Remove(entity);
        }
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
        var curTime = _timing.CurTime;
        foreach (var (tile, reservedUntil) in _reservedTiles.ToArray())
        {
            if (curTime >= reservedUntil)
                _reservedTiles.Remove(tile);
        }

        for (var jobIndex = _jobs.Count - 1; jobIndex >= 0; jobIndex--)
        {
            var job = _jobs[jobIndex];
            while (job.NextPod < job.Pods.Count && curTime >= job.Pods[job.NextPod].LaunchAt)
            {
                var queued = job.Pods[job.NextPod++];
                LaunchQueuedPod(queued, job.Request);
            }

            if (job.NextPod >= job.Pods.Count)
                _jobs.RemoveAt(jobIndex);
        }

        var query = EntityQueryEnumerator<SupplyDropPodComponent>();
        while (query.MoveNext(out var uid, out var dropPod))
        {
            if (!dropPod.Landed)
                continue;

            dropPod.OpenTimeRemaining -= TimeSpan.FromSeconds(frameTime);
            if (dropPod.OpenTimeRemaining > TimeSpan.Zero)
                continue;

            if (Container.TryGetContainer(uid, dropPod.DeploySlotId, out var container))
                Container.EmptyContainer(container, true);

            QueueDel(uid);
        }
    }

    private readonly record struct ReservedTile(EntityUid Grid, Vector2i Indices);
    private readonly record struct LandingTile(MapCoordinates Coordinates, ReservedTile Tile);

    private sealed class PreparedOrbitalDrop(
        RMCOrbitalDropRequest request,
        List<EntityUid> existingPayload,
        List<EntityUid> spawnedPayload,
        List<List<EntityUid>> podPayloads,
        List<EntityUid> preparedPods,
        List<LandingTile> landingTiles)
    {
        public readonly RMCOrbitalDropRequest Request = request;
        public readonly List<EntityUid> ExistingPayload = existingPayload;
        public readonly List<EntityUid> SpawnedPayload = spawnedPayload;
        public readonly List<List<EntityUid>> PodPayloads = podPayloads;
        public readonly List<EntityUid> PreparedPods = preparedPods;
        public readonly List<LandingTile> LandingTiles = landingTiles;
    }

    private readonly record struct QueuedDropPod(
        EntityUid Pod,
        MapCoordinates Coordinates,
        ReservedTile Tile,
        List<EntityUid> PendingEntities,
        List<EntityCoordinates> LaunchCoordinates,
        TimeSpan LaunchAt,
        TimeSpan DropAt);

    private sealed class OrbitalDropJob(List<QueuedDropPod> pods, RMCOrbitalDropRequest request)
    {
        public readonly List<QueuedDropPod> Pods = pods;
        public readonly RMCOrbitalDropRequest Request = request;
        public int NextPod;
    }
}
