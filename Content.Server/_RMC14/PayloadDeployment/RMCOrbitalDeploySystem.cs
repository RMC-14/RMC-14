using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._RMC14.CrashLand;
using Content.Shared._RMC14.ParaDrop;
using Content.Shared._RMC14.PayloadDeployment;
using Content.Shared._RMC14.PayloadDeployment.Systems;
using Content.Shared._RMC14.SupplyDrop;
using Content.Shared.Damage;
using Content.Shared.GameTicking;
using Content.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.PayloadDeployment;

public sealed class RMCOrbitalDeploySystem : SharedRMCOrbitalDeploySystem
{
    private static readonly EntProtoId? DirectDropLandingEffect = "RMCEffectAlert";
    private static readonly SoundSpecifier DirectDropLaunchSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/bamf.ogg");
    private static readonly SoundSpecifier DirectDropArrivingSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/Techpod/techpod_drill.ogg");

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCrashLandSystem _crashLand = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly RMCPayloadDeploymentSystem _payloadDeployment = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly List<OrbitalDropJob> _jobs = [];
    private readonly HashSet<EntityUid> _reservedPayloads = [];
    private readonly Dictionary<ReservedTile, TimeSpan> _reservedTiles = new();
    private readonly DamageSpecifier _directDropLandingDamage = new() { DamageDict = { ["Blunt"] = 5000 } };

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
        var totalDeliveries = 0;

        for (var requestIndex = 0; requestIndex < requests.Count; requestIndex++)
        {
            var request = requests[requestIndex];
            if (!ValidateRequest(request, out var existingPayload, out var requestPayload))
            {
                CleanupPreparedDrops(prepared);
                return new RMCPayloadDeploymentResult(RMCPayloadDeploymentFailure.InvalidSettings, requestIndex);
            }

            foreach (var entity in existingPayload)
            {
                if (batchPayload.Add(entity))
                    continue;

                CleanupPreparedDrops(prepared);
                return new RMCPayloadDeploymentResult(RMCPayloadDeploymentFailure.InvalidPayload, requestIndex);
            }

            var requestDeliveries = request.UseDropPods ? request.PodCount : requestPayload;
            if (totalPayload + requestPayload > RMCPayloadDeploymentLimits.MaxPayload ||
                totalDeliveries + requestDeliveries > RMCPayloadDeploymentLimits.MaxOrbitalDrops)
            {
                CleanupPreparedDrops(prepared);
                return new RMCPayloadDeploymentResult(RMCPayloadDeploymentFailure.InvalidSettings, requestIndex);
            }

            totalPayload += requestPayload;
            totalDeliveries += requestDeliveries;
            if (!TryPrepareOrbitalDrop(request,
                    existingPayload,
                    requestPayload,
                    batchTiles,
                    out var drop,
                    out var failure,
                    out var assignedLandings))
            {
                CleanupPreparedDrops(prepared);
                return new RMCPayloadDeploymentResult(
                    failure,
                    requestIndex,
                    requestDeliveries,
                    assignedLandings);
            }

            prepared.Add(drop);
            foreach (var landing in drop.LandingTiles)
            {
                foreach (var tile in landing.Tiles)
                {
                    batchTiles.Add(tile);
                }
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
        [NotNullWhen(true)] out PreparedOrbitalDrop? prepared,
        out RMCPayloadDeploymentFailure failure,
        out int assignedLandings)
    {
        prepared = null;
        failure = RMCPayloadDeploymentFailure.None;
        assignedLandings = 0;
        var spawnedPayload = new List<EntityUid>();
        foreach (var prototypePayload in request.Prototypes)
        {
            if (!_prototypes.TryIndex(prototypePayload.Prototype, out var prototype) ||
                prototype.Abstract ||
                prototype.HasComponent<OccluderComponent>(EntityManager.ComponentFactory))
            {
                CleanupEntities(spawnedPayload);
                failure = RMCPayloadDeploymentFailure.InvalidPrototype;
                return false;
            }

            for (var i = 0; i < prototypePayload.Quantity; i++)
            {
                var spawned = Spawn(prototypePayload.Prototype);
                spawnedPayload.Add(spawned);
                _payloadDeployment.PreparePrototypePayload(spawned, prototype);
            }
        }

        var payload = new List<EntityUid>(totalPayload);
        payload.AddRange(existingPayload);
        payload.AddRange(spawnedPayload);

        var podPayloads = new List<List<EntityUid>>();
        var preparedPods = new List<EntityUid>();
        var podContainers = new List<BaseContainer>();
        if (request.UseDropPods)
        {
            for (var i = 0; i < request.PodCount; i++)
            {
                podPayloads.Add([]);
            }

            for (var i = 0; i < payload.Count; i++)
            {
                podPayloads[i % request.PodCount].Add(payload[i]);
            }

            for (var i = 0; i < request.PodCount; i++)
            {
                if (!TryCreateOrbitalDropPod(request.TimeToOpen, out var pod))
                {
                    CleanupEntities(preparedPods);
                    CleanupEntities(spawnedPayload);
                    failure = RMCPayloadDeploymentFailure.PodPreparationFailed;
                    return false;
                }

                preparedPods.Add(pod.Owner);
                podContainers.Add(Container.EnsureContainer<Container>(pod.Owner, pod.Comp.DeploySlotId));
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
        }

        var deliveries = request.UseDropPods ? preparedPods : payload;
        if (!_mapManager.TryFindGridAt(request.Target, out var grid, out var gridComponent))
        {
            CleanupEntities(preparedPods);
            CleanupEntities(spawnedPayload);
            failure = RMCPayloadDeploymentFailure.InvalidTarget;
            return false;
        }

        if (!TryFindLandingTiles(request, (grid, gridComponent), deliveries, batchTiles, out var landingTiles, out assignedLandings))
        {
            CleanupEntities(preparedPods);
            CleanupEntities(spawnedPayload);
            failure = RMCPayloadDeploymentFailure.InsufficientLandingTiles;
            return false;
        }

        if (request.UseDropPods)
        {
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
        }

        prepared = new PreparedOrbitalDrop(
            request,
            existingPayload,
            spawnedPayload,
            payload,
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
        var stagingOrigin = _payloadDeployment.AllocateStagingGroup();
        var previousDropAt = TimeSpan.Zero;
        var deliveries = prepared.Request.UseDropPods ? prepared.PreparedPods : prepared.Payload;
        var queuedDrops = new List<QueuedOrbitalDrop>(deliveries.Count);
        for (var i = 0; i < deliveries.Count; i++)
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
            var stagingOffset = landing.Coordinates.Position - prepared.Request.Target.Position;
            var staging = _payloadDeployment.GetStagingCoordinates(stagingOrigin + stagingOffset);
            var pendingEntities = prepared.Request.UseDropPods
                ? prepared.PodPayloads[i].Where(existingPayloadSet.Contains).ToList()
                : [];
            var reservationUntil = dropAt +
                TimeSpan.FromSeconds(prepared.Request.DropDuration +
                    (prepared.Request.UseDropPods ? prepared.Request.TimeToOpen : 0));
            foreach (var tile in landing.Tiles)
            {
                _reservedTiles[tile] = reservationUntil;
            }

            queuedDrops.Add(new QueuedOrbitalDrop(
                deliveries[i],
                prepared.Request.UseDropPods,
                existingPayloadSet.Contains(deliveries[i]),
                landing.Coordinates,
                landing.Tiles,
                landing.Rotation,
                staging,
                pendingEntities,
                launchAt,
                dropAt));
        }

        _jobs.Add(new OrbitalDropJob(queuedDrops, prepared.Request));
    }

    private bool ValidateRequest(RMCOrbitalDropRequest request, out List<EntityUid> existingPayload, out int totalPayload)
    {
        existingPayload = request.Entities.Distinct().ToList();
        totalPayload = existingPayload.Count;

        if (!IsValidTiming(request.ArrivalDelay) ||
            !IsValidTiming(request.DropDuration) ||
            request.UseDropPods && !IsValidTiming(request.TimeToOpen) ||
            !IsValidTiming(request.LaunchInterval) ||
            !IsValidTiming(request.DropInterval) ||
            !IsValidTiming(request.DropIntervalVariation) ||
            !float.IsFinite(request.Target.X) || !float.IsFinite(request.Target.Y) ||
            request.LandingRadius < 0 || request.LandingRadius > RMCPayloadDeploymentLimits.MaxLandingRadius ||
            request.UseDropPods &&
            (request.PodCount <= 0 || request.PodCount > RMCPayloadDeploymentLimits.MaxOrbitalDrops))
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
               (!request.UseDropPods || request.PodCount <= totalPayload);
    }

    private static bool IsValidTiming(float value)
    {
        return value is >= 0 and <= RMCPayloadDeploymentLimits.MaxTimingSeconds;
    }

    private bool TryFindLandingTiles(RMCOrbitalDropRequest request, Entity<MapGridComponent> grid,
        IReadOnlyList<EntityUid> deliveries, HashSet<ReservedTile> batchTiles,
        out List<LandingTile> selected, out int assignedCount)
    {
        selected = [];
        assignedCount = 0;
        var centerCoordinates = _transform.ToCoordinates(request.Target);
        var center = _map.LocalToTile(grid, grid.Comp, centerCoordinates);
        var centerPosition = _map.GridTileToLocal(grid, grid.Comp, center).Position;
        var candidates = new List<Vector2i>();
        var circleArea = new Circle(centerPosition, request.LandingRadius * grid.Comp.TileSize);
        foreach (var tileRef in _map.GetLocalTilesIntersecting(grid, grid.Comp, circleArea, false))
        {
            candidates.Add(tileRef.GridIndices);
        }

        _random.Shuffle(candidates);
        var assignedTiles = new HashSet<ReservedTile>(batchTiles);
        foreach (var delivery in deliveries)
        {
            LandingTile? landing = null;
            foreach (var candidate in candidates)
            {
                if (!_crashLand.TryGetLandableFootprint(delivery,
                        grid,
                        candidate,
                        request.IgnoreParadropRestrictions,
                        out var footprint))
                {
                    continue;
                }

                var tiles = new List<ReservedTile>(footprint.Count);
                var unavailable = false;
                foreach (var tileRef in footprint)
                {
                    var tile = new ReservedTile(grid.Owner, tileRef.GridIndices);
                    if (assignedTiles.Contains(tile) ||
                        _reservedTiles.TryGetValue(tile, out var reservedUntil) &&
                        reservedUntil > _timing.CurTime)
                    {
                        unavailable = true;
                        break;
                    }

                    tiles.Add(tile);
                }

                if (unavailable)
                    continue;

                var coordinates = _transform.ToMapCoordinates(_map.GridTileToLocal(grid, grid.Comp, candidate));
                landing = new LandingTile(coordinates, tiles, _transform.GetWorldRotation(delivery));
                break;
            }

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

    private void LaunchQueuedPod(QueuedOrbitalDrop queued, RMCOrbitalDropRequest request)
    {
        if (TerminatingOrDeleted(queued.Entity) || EntityManager.IsQueuedForDeletion(queued.Entity))
        {
            ReleasePayloadReservations(queued.PendingEntities);
            ReleaseTileReservations(queued.Tiles);
            return;
        }

        if (!TryComp(queued.Entity, out SupplyDropPodComponent? podComponent) ||
            !Container.TryGetContainer(queued.Entity, podComponent.DeploySlotId, out var podContainer))
        {
            ReleasePayloadReservations(queued.PendingEntities);
            ReleaseTileReservations(queued.Tiles);
            QueueDel(queued.Entity);
            return;
        }

        var launchCoordinates = new List<EntityCoordinates>();
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
            QueueDel(queued.Entity);
            ReleaseTileReservations(queued.Tiles);
            return;
        }

        var timeUntilDrop = Math.Max(0, (float) (queued.DropAt - _timing.CurTime).TotalSeconds);
        var reservedUntil = _timing.CurTime +
            TimeSpan.FromSeconds(timeUntilDrop + request.DropDuration + request.TimeToOpen);
        foreach (var tile in queued.Tiles)
        {
            _reservedTiles[tile] = reservedUntil;
        }

        _transform.SetWorldRotation(queued.Entity, queued.Rotation);
        LaunchOrbitalDropPod(
            queued.Entity,
            queued.Coordinates,
            timeUntilDrop,
            request.DropDuration,
            request.UseParachute,
            launchCoordinates,
            stagingCoordinates: queued.StagingCoordinates,
            showLandingWarning: request.ShowLandingWarning);
    }

    private void LaunchQueuedDirectDrop(QueuedOrbitalDrop queued, RMCOrbitalDropRequest request)
    {
        if (queued.Existing)
            _reservedPayloads.Remove(queued.Entity);

        if (TerminatingOrDeleted(queued.Entity) ||
            EntityManager.IsQueuedForDeletion(queued.Entity) ||
            IsBeingSupplyDropped(queued.Entity))
        {
            ReleaseTileReservations(queued.Tiles);

            if (queued.Existing)
                return;

            _payloadDeployment.CancelPrototypePayload(queued.Entity);
            if (!TerminatingOrDeleted(queued.Entity) &&
                !EntityManager.IsQueuedForDeletion(queued.Entity))
            {
                QueueDel(queued.Entity);
            }

            return;
        }

        var launchCoordinates = queued.Existing
            ? new List<EntityCoordinates> { _transform.GetMoverCoordinates(queued.Entity) }
            : [];
        var timeUntilDrop = Math.Max(0, (float) (queued.DropAt - _timing.CurTime).TotalSeconds);
        var reservedUntil = _timing.CurTime + TimeSpan.FromSeconds(timeUntilDrop + request.DropDuration);
        foreach (var tile in queued.Tiles)
        {
            _reservedTiles[tile] = reservedUntil;
        }

        _transform.SetWorldRotation(queued.Entity, queued.Rotation);
        // Play at the original locations for observers, then in staging for payload entities moved to the temporary map.
        foreach (var launch in launchCoordinates.Distinct())
        {
            _audio.PlayPvs(DirectDropLaunchSound, launch);
        }

        SupplyDrop.LaunchSupplyDrop(queued.Entity,
            queued.Coordinates,
            timeUntilDrop,
            request.DropDuration,
            TimeSpan.Zero,
            _directDropLandingDamage,
            request.ShowLandingWarning ? DirectDropLandingEffect : null,
            DirectDropArrivingSound,
            0,
            request.UseParachute,
            queued.StagingCoordinates,
            false);

        _audio.PlayPvs(DirectDropLaunchSound, _transform.GetMoverCoordinates(queued.Entity));
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

    private void ReleaseTileReservations(IEnumerable<ReservedTile> tiles)
    {
        foreach (var tile in tiles)
        {
            _reservedTiles.Remove(tile);
        }
    }

    private void CleanupEntities(IEnumerable<EntityUid> entities)
    {
        foreach (var entity in entities)
        {
            _payloadDeployment.CancelPrototypePayload(entity);
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
            while (job.NextDrop < job.Drops.Count && curTime >= job.Drops[job.NextDrop].LaunchAt)
            {
                var queued = job.Drops[job.NextDrop++];
                if (queued.DropPod)
                    LaunchQueuedPod(queued, job.Request);
                else
                    LaunchQueuedDirectDrop(queued, job.Request);
            }

            if (job.NextDrop >= job.Drops.Count)
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
            {
                foreach (var entity in Container.EmptyContainer(container, true))
                {
                    _payloadDeployment.CompletePrototypePayload(entity);
                }
            }

            QueueDel(uid);
        }
    }

    private readonly record struct ReservedTile(EntityUid Grid, Vector2i Indices);
    private readonly record struct LandingTile(MapCoordinates Coordinates, List<ReservedTile> Tiles, Angle Rotation);

    private sealed class PreparedOrbitalDrop(
        RMCOrbitalDropRequest request,
        List<EntityUid> existingPayload,
        List<EntityUid> spawnedPayload,
        List<EntityUid> payload,
        List<List<EntityUid>> podPayloads,
        List<EntityUid> preparedPods,
        List<LandingTile> landingTiles)
    {
        public readonly RMCOrbitalDropRequest Request = request;
        public readonly List<EntityUid> ExistingPayload = existingPayload;
        public readonly List<EntityUid> SpawnedPayload = spawnedPayload;
        public readonly List<EntityUid> Payload = payload;
        public readonly List<List<EntityUid>> PodPayloads = podPayloads;
        public readonly List<EntityUid> PreparedPods = preparedPods;
        public readonly List<LandingTile> LandingTiles = landingTiles;
    }

    private readonly record struct QueuedOrbitalDrop(
        EntityUid Entity,
        bool DropPod,
        bool Existing,
        MapCoordinates Coordinates,
        List<ReservedTile> Tiles,
        Angle Rotation,
        MapCoordinates StagingCoordinates,
        List<EntityUid> PendingEntities,
        TimeSpan LaunchAt,
        TimeSpan DropAt);

    private sealed class OrbitalDropJob(List<QueuedOrbitalDrop> drops, RMCOrbitalDropRequest request)
    {
        public readonly List<QueuedOrbitalDrop> Drops = drops;
        public readonly RMCOrbitalDropRequest Request = request;
        public int NextDrop;
    }
}
