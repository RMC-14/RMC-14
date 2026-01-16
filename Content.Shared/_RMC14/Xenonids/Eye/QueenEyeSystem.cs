using System.Numerics;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Watch;
using Content.Shared.Coordinates;
using Content.Shared.Mind;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Threading;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Eye;

public sealed class QueenEyeSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IParallelManager _parallel = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedXenoWatchSystem _xenoWatch = default!;

    private SeedJob _seedJob;
    private ViewJob _job;

    private readonly HashSet<Entity<QueenEyeVisionComponent>> _seeds = new();

    private readonly HashSet<Vector2i> _singleTiles = new();

    public override void Initialize()
    {
        base.Initialize();

        _seedJob = new SeedJob
        {
            System = this,
        };

        _job = new ViewJob()
        {
            EntManager = EntityManager,
            Maps = _map,
            System = this,
            VisibleTiles = _singleTiles,
        };

        SubscribeLocalEvent<QueenEyeActionComponent, MapInitEvent>(OnQueenEyeActionMapInit);
        SubscribeLocalEvent<QueenEyeActionComponent, ComponentRemove>(OnQueenEyeActionRemove);
        SubscribeLocalEvent<QueenEyeActionComponent, EntityTerminatingEvent>(OnQueenEyeActionTerminating);
        SubscribeLocalEvent<QueenEyeActionComponent, QueenEyeActionEvent>(OnQueenEyeAction);
        SubscribeLocalEvent<QueenEyeActionComponent, GetVisMaskEvent>(OnQueenEyeActionGetVisMask);
        SubscribeLocalEvent<QueenEyeActionComponent, XenoWatchEvent>(OnQueenEyeActionWatch);
        SubscribeLocalEvent<QueenEyeActionComponent, XenoUnwatchEvent>(OnQueenEyeActionUnwatch);
        SubscribeLocalEvent<QueenEyeActionComponent, XenoOvipositorChangedEvent>(OnQueenEyeOvipositorChanged);

        SubscribeLocalEvent<QueenEyeComponent, XenoUnwatchEvent>(OnQueenEyeUnwatch);
    }

    private void OnQueenEyeActionMapInit(Entity<QueenEyeActionComponent> ent, ref MapInitEvent args)
    {
        _eye.RefreshVisibilityMask(ent.Owner);
    }

    private void OnQueenEyeActionRemove(Entity<QueenEyeActionComponent> ent, ref ComponentRemove args)
    {
        RemoveQueenEye(ent);
    }

    private void OnQueenEyeActionTerminating(Entity<QueenEyeActionComponent> ent, ref EntityTerminatingEvent args)
    {
        RemoveQueenEye(ent);
    }

    private void OnQueenEyeAction(Entity<QueenEyeActionComponent> ent, ref QueenEyeActionEvent args)
    {
        if (RemoveQueenEye(ent))
            return;

        if (_net.IsClient)
            return;

        if (!TryComp(ent, out EyeComponent? eye))
            return;

        ent.Comp.Eye = SpawnAtPosition(ent.Comp.Spawn, ent.Owner.ToCoordinates());
        Dirty(ent);

        var eyeComp = EnsureComp<QueenEyeComponent>(ent.Comp.Eye.Value);
        eyeComp.Queen = ent;
        Dirty(ent.Comp.Eye.Value, eyeComp);

        _eye.SetPvsScale((ent, eye), ent.Comp.EyePvsScale);
        _eye.SetTarget(ent, ent.Comp.Eye, eye);
        _eye.SetDrawFov(ent, false);
        _mover.SetRelay(ent, ent.Comp.Eye.Value);
    }

    private void OnQueenEyeActionGetVisMask(Entity<QueenEyeActionComponent> ent, ref GetVisMaskEvent args)
    {
        if (!_mind.TryGetMind(ent.Owner, out _, out var mind) ||
            !HasComp<QueenEyeComponent>(mind.VisitingEntity))
        {
            return;
        }

        args.VisibilityMask |= (int) ent.Comp.Visibility;
    }

    private void OnQueenEyeActionWatch(Entity<QueenEyeActionComponent> ent, ref XenoWatchEvent args)
    {
        if (ent.Comp.Eye is not { } eye)
            return;

        _xenoWatch.SetWatching(eye, args.Watching);
    }

    private void OnQueenEyeActionUnwatch(Entity<QueenEyeActionComponent> ent, ref XenoUnwatchEvent args)
    {
        if (ent.Comp.Eye is not { } eye)
            return;

        RemCompDeferred<XenoWatchingComponent>(eye);
    }

    private void OnQueenEyeOvipositorChanged(Entity<QueenEyeActionComponent> ent, ref XenoOvipositorChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.Attached)
            return;

        RemoveQueenEye(ent);
    }

    private void OnQueenEyeUnwatch(Entity<QueenEyeComponent> ent, ref XenoUnwatchEvent args)
    {
        if (ent.Comp.Queen is not { } queen)
            return;

        _eye.SetTarget(queen, ent);
    }

    /// <param name="expansionSize">How much to expand the bounds before to find vision intersecting it. Makes this the largest vision size + 1 tile.</param>
    public void GetView(Entity<BroadphaseComponent, MapGridComponent> grid, Box2Rotated worldBounds, HashSet<Vector2i> visibleTiles, float expansionSize = 29)
    {
        _seeds.Clear();

        // TODO: Would be nice to be able to run this while running the other stuff.
        _seedJob.Grid = (grid.Owner, grid.Comp2);
        var invMatrix = _transform.GetInvWorldMatrix(grid);
        var enlargedLocalAabb = invMatrix.TransformBox(worldBounds.Enlarged(expansionSize));
        _seedJob.ExpandedBounds = enlargedLocalAabb;
        _parallel.ProcessNow(_seedJob);
        _job.Data.Clear();

        foreach (var seed in _seeds)
        {
            _job.Data.Add(seed);
        }

        if (_seeds.Count == 0)
            return;

        _job.Grid = (grid.Owner, grid.Comp2);
        _job.VisibleTiles = visibleTiles;
        _parallel.ProcessNow(_job, _job.Data.Count);
    }

    /// <summary>
    /// Returns whether a tile is accessible based on vision.
    /// </summary>
    private bool IsAccessible(Entity<BroadphaseComponent, MapGridComponent> grid, Vector2i tile, float expansionSize = 29)
    {
        _seeds.Clear();
        var localBounds = _entityLookup.GetLocalBounds(tile, grid.Comp2.TileSize);
        var expandedBounds = localBounds.Enlarged(expansionSize);

        _seedJob.Grid = (grid.Owner, grid.Comp2);
        _seedJob.ExpandedBounds = expandedBounds;
        _parallel.ProcessNow(_seedJob);
        _job.Data.Clear();

        foreach (var seed in _seeds)
        {
            _job.Data.Add(seed);
        }

        if (_seeds.Count == 0)
            return false;

        _singleTiles.Clear();
        _job.Grid = (grid.Owner, grid.Comp2);
        _job.VisibleTiles = _singleTiles;
        _parallel.ProcessNow(_job, _job.Data.Count);

        return _job.VisibleTiles.Contains(tile);
    }

    private bool RemoveQueenEye(Entity<QueenEyeActionComponent> ent)
    {
        if (ent.Comp.Eye == null)
            return false;

        _eye.SetTarget(ent, null);
        _eye.SetPvsScale(ent.Owner, ent.Comp.PvsScale);
        _eye.SetDrawFov(ent, true);

        if (_net.IsServer && HasComp<QueenEyeComponent>(ent.Comp.Eye))
            QueueDel(ent.Comp.Eye);

        ent.Comp.Eye = null;
        Dirty(ent);

        RemComp<RelayInputMoverComponent>(ent);

        var ev = new QueenEyeActionUpdated(ent);
        RaiseLocalEvent(ent, ref ev);

        return true;
    }

    public bool IsInQueenEye(Entity<QueenEyeActionComponent?> queen)
    {
        return Resolve(queen, ref queen.Comp, false) && queen.Comp.Eye != null;
    }

    public bool CanSeeTarget(Entity<QueenEyeActionComponent?> queen, EntityUid target)
    {
        if (!Resolve(queen, ref queen.Comp, false) ||
            queen.Comp.Eye == null)
        {
            return false;
        }

        var targetTransform = Transform(target);
        if (!TryComp(targetTransform.GridUid, out BroadphaseComponent? broadphase) ||
            !TryComp(targetTransform.GridUid, out MapGridComponent? grid))
        {
            return false;
        }

        var targetTile = _map.LocalToTile(targetTransform.GridUid.Value, grid, targetTransform.Coordinates);
        return IsAccessible((targetTransform.GridUid.Value, broadphase, grid), targetTile);
    }

    public bool CanSeeTarget(Entity<QueenEyeActionComponent?> queen, EntityCoordinates target)
    {
        if (!Resolve(queen, ref queen.Comp, false) ||
            queen.Comp.Eye == null)
        {
            return false;
        }

        if (_transform.GetGrid(target) is not { } gridId ||
            !TryComp(gridId, out BroadphaseComponent? broadphase) ||
            !TryComp(gridId, out MapGridComponent? grid))
        {
            return false;
        }

        var targetTile = _map.CoordinatesToTile(gridId, grid, target);
        return IsAccessible((gridId, broadphase, grid), targetTile);
    }

    /// <summary>
    /// Gets the relevant vision seeds for later.
    /// </summary>
    private record struct SeedJob : IRobustJob
    {
        public required QueenEyeSystem System;

        public Entity<MapGridComponent> Grid;
        public Box2 ExpandedBounds;

        public void Execute()
        {
            System._entityLookup.GetLocalEntitiesIntersecting(Grid.Owner, ExpandedBounds, System._seeds, flags: LookupFlags.All | LookupFlags.Approximate);
        }
    }

    private record struct ViewJob() : IParallelRobustJob
    {
        public int BatchSize => 1;

        public required IEntityManager EntManager;
        public required SharedMapSystem Maps;
        public required QueenEyeSystem System;

        public Entity<MapGridComponent> Grid;
        public List<Entity<QueenEyeVisionComponent>> Data = new();

        public required HashSet<Vector2i> VisibleTiles;

        public void Execute(int index)
        {
            var seed = Data[index];
            var seedXform = EntManager.GetComponent<TransformComponent>(seed);
            var squircles = Maps.GetLocalTilesIntersecting(Grid.Owner,
                Grid.Comp,
                Box2.CenteredAround(System._transform.GetWorldPosition(seedXform), new Vector2(seed.Comp.Range)),
                ignoreEmpty: false);

            lock (VisibleTiles)
            {
                foreach (var tile in squircles)
                {
                    VisibleTiles.Add(tile.GridIndices);
                }
            }
        }
    }
}
