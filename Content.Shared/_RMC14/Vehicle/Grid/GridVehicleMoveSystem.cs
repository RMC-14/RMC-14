using System;
using System.Numerics;
using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Destructible;
using Content.Shared.Doors.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Vehicle.Components;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Power;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.Xenonids;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Physics;

namespace Content.Shared.Vehicle;

public sealed partial class GridVehicleMoverSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem transform = default!;
    [Dependency] private readonly SharedMapSystem map = default!;
    [Dependency] private readonly SharedPhysicsSystem physics = default!;
    [Dependency] private readonly EntityLookupSystem lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;
    [Dependency] private readonly VehicleWheelSystem _wheels = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedRMCPowerSystem _rmcPower = default!;

    private EntityQuery<MapGridComponent> gridQ;
    private EntityQuery<PhysicsComponent> physicsQ;
    private EntityQuery<FixturesComponent> fixtureQ;

    private const float Clearance = PhysicsConstants.PolygonRadius * 0.75f;
    private const double MobCollisionDamage = 8;
    private static readonly TimeSpan MobCollisionKnockdown = TimeSpan.FromSeconds(1.5);
    private static readonly TimeSpan MobCollisionCooldown = TimeSpan.FromSeconds(0.75);
    private static readonly ProtoId<DamageTypePrototype> CollisionDamageType = "Blunt";
    private const int GridVehicleStaticBlockerMask =
        (int) (CollisionGroup.Impassable |
               CollisionGroup.HighImpassable |
               CollisionGroup.LowImpassable |
               CollisionGroup.MidImpassable |
               CollisionGroup.BarricadeImpassable |
               CollisionGroup.DropshipImpassable);
    private const CollisionGroup GridVehiclePushHardBlockMask =
        CollisionGroup.Impassable |
        CollisionGroup.HighImpassable |
        CollisionGroup.LowImpassable |
        CollisionGroup.MidImpassable |
        CollisionGroup.DropshipImpassable;
    private const float PushTileBlockFraction = 0.005f;
    private const float PushOverlapEpsilon = 0.05f;
    private const float PushAxisHysteresis = 0.05f;
    private const float PushWallSkin = 0.1f;
    private const float PushWallOverlapArea = 0.01f;
    private const float MovementFixedStep = 1f / 60f;
    private const int MaxFixedStepsPerFrame = 6;
    private const float ClientSmoothingSnapDistance = 1.25f;
    private const float ClientSmoothingRate = 22f;


    public static readonly List<(EntityUid grid, Vector2i tile)> DebugTestedTiles = new();
    public static readonly List<DebugCollisionProbe> DebugCollisionProbes = new();
    public static readonly List<DebugCollision> DebugCollisions = new();
    public static readonly List<DebugMovementDecision> DebugMovementDecisions = new();
    public static bool CollisionDebugEnabled { get; set; }
    public static bool MovementDebugEnabled { get; set; }

    private readonly Dictionary<EntityUid, TimeSpan> _lastMobCollision = new();
    private readonly Dictionary<EntityUid, bool> _hardState = new();
    private readonly Dictionary<EntityUid, bool> _lastMobPushAxis = new();
    private readonly Dictionary<EntityUid, float> _movementAccumulator = new();
    private readonly Dictionary<EntityUid, EntityUid> _activeXenoPushers = new();
    private readonly HashSet<EntityUid> _directMoveBlockers = new();
    private readonly HashSet<EntityUid> _pushIgnoredEntities = new();

    private enum VehicleCollisionClass : byte
    {
        Ignore = 0,
        SoftMob = 1,
        Breakable = 2,
        Hard = 3,
    }

    public enum DebugMovementDecisionKind : byte
    {
        DirectClear = 0,
        DirectBlocked = 1,
        LaneCorrection = 2,
        LaneCorrectionFailed = 3,
        ForwardAfterCorrection = 4,
        ForwardBlocked = 5,
    }

    public readonly record struct DebugCollision(
        EntityUid Tested,
        EntityUid Blocker,
        Box2 TestedAabb,
        Box2 BlockerAabb,
        float Distance,
        float Skin,
        float Clearance,
        MapId Map);

    public readonly record struct DebugCollisionProbe(
        EntityUid Tested,
        Box2 TestedAabb,
        Box2 MovementAabb,
        Box2Rotated FixtureBounds,
        Box2Rotated MovementBounds,
        Vector2 Position,
        Angle Rotation,
        bool Blocked,
        bool ApplyEffects,
        MapId Map);

    public readonly record struct DebugMovementDecision(
        EntityUid Vehicle,
        EntityUid Grid,
        Vector2 Start,
        Vector2 End,
        Vector2 MoveDirection,
        DebugMovementDecisionKind Kind,
        bool Success);

    public override void Initialize()
    {
        base.Initialize();
        gridQ = GetEntityQuery<MapGridComponent>();
        physicsQ = GetEntityQuery<PhysicsComponent>();
        fixtureQ = GetEntityQuery<FixturesComponent>();
        SubscribeLocalEvent<GridVehicleMoverComponent, ComponentStartup>(OnMoverStartup);
        SubscribeLocalEvent<GridVehicleMoverComponent, ComponentShutdown>(OnMoverShutdown);
        SubscribeLocalEvent<GridVehicleMoverComponent, MoveEvent>(OnMoverMove);
        SubscribeLocalEvent<GridVehicleMoverComponent, ReAnchorEvent>(OnMoverReAnchor);
        SubscribeLocalEvent<GridVehicleMoverComponent, VehicleCanRunEvent>(OnMoverCanRun);
        SubscribeLocalEvent<GridVehicleMoverComponent, PreventCollideEvent>(OnMoverPreventCollide);
    }

    private void OnMoverStartup(Entity<GridVehicleMoverComponent> ent, ref ComponentStartup args)
    {
        TrySyncMoverToCurrentGrid(ent, centerOnTile: true, force: true);
    }

    private void OnMoverShutdown(Entity<GridVehicleMoverComponent> ent, ref ComponentShutdown args)
    {
        _hardState.Remove(ent.Owner);
        _movementAccumulator.Remove(ent.Owner);
        _activeXenoPushers.Remove(ent.Owner);
    }

    private void OnMoverMove(Entity<GridVehicleMoverComponent> ent, ref MoveEvent args)
    {
        if (!args.ParentChanged)
            return;

        TrySyncMoverToCurrentGrid(ent, centerOnTile: false);
    }

    private void OnMoverReAnchor(Entity<GridVehicleMoverComponent> ent, ref ReAnchorEvent args)
    {
        TrySyncMoverToCurrentGrid(ent, centerOnTile: false);
    }

    // Vehicle traversal can change grids through several engine paths. Keep all resync logic in one place.
    private bool TrySyncMoverToCurrentGrid(
        Entity<GridVehicleMoverComponent> ent,
        bool centerOnTile,
        TransformComponent? xform = null,
        bool force = false)
    {
        var uid = ent.Owner;
        xform ??= Transform(uid);

        if (xform.GridUid is not { } grid || !gridQ.TryComp(grid, out var gridComp))
        {
            if (ent.Comp.SyncedGrid == null)
                return false;

            ent.Comp.SyncedGrid = null;
            ent.Comp.CurrentSpeed = 0f;
            ent.Comp.PushDirection = Vector2i.Zero;
            ent.Comp.IsCommittedToMove = false;
            ent.Comp.IsPushMove = false;
            ent.Comp.IsMoving = false;
            _hardState[uid] = true;
            _movementAccumulator[uid] = 0f;
            Dirty(uid, ent.Comp);
            return true;
        }

        if (!force && ent.Comp.SyncedGrid == grid)
            return false;

        var coords = xform.Coordinates.WithEntityId(grid, transform, EntityManager);
        var tile = map.TileIndicesFor(grid, gridComp, coords);

        ent.Comp.SyncedGrid = grid;
        ent.Comp.CurrentTile = tile;
        ent.Comp.TargetTile = tile;
        ent.Comp.Position = centerOnTile
            ? new Vector2(tile.X + 0.5f, tile.Y + 0.5f)
            : coords.Position;
        ent.Comp.TargetPosition = ent.Comp.Position;
        ent.Comp.CurrentSpeed = 0f;
        ent.Comp.PushDirection = Vector2i.Zero;
        ent.Comp.NextPushTime = TimeSpan.Zero;
        ent.Comp.NextTurnTime = TimeSpan.Zero;
        ent.Comp.InPlaceTurnBlockUntil = TimeSpan.Zero;
        ent.Comp.IsCommittedToMove = false;
        ent.Comp.IsPushMove = false;
        ent.Comp.IsMoving = false;
        _hardState[uid] = true;
        _movementAccumulator[uid] = 0f;

        Dirty(uid, ent.Comp);
        return true;
    }

    private void OnMoverCanRun(Entity<GridVehicleMoverComponent> ent, ref VehicleCanRunEvent args)
    {
        if (!args.CanRun)
            return;

        if (!TryComp(ent.Owner, out VehicleComponent? vehicle) || vehicle.Operator is not { } operatorUid)
            return;

        if (!HasComp<XenoComponent>(operatorUid))
            return;

        args.CanRun = false;
    }

    private void OnMoverPreventCollide(Entity<GridVehicleMoverComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp(ent.Owner, out VehicleComponent? vehicle) || vehicle.MovementKind != VehicleMovementKind.Grid)
            return;

        if (args.OtherEntity == ent.Owner)
            return;

        if (TryComp(args.OtherEntity, out VehicleRideSurfaceRiderComponent? rider) && rider.Vehicle == ent.Owner)
        {
            args.Cancelled = true;
            return;
        }

        if (args.OtherBody.BodyType != BodyType.Static)
            return;

        if (IsNormallyMobPassable(args.OtherFixture))
        {
            args.Cancelled = true;
            return;
        }

        if ((args.OtherFixture.CollisionLayer & GridVehicleStaticBlockerMask) == 0)
            return;

        args.Cancelled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (CollisionDebugEnabled)
        {
            DebugTestedTiles.Clear();
            DebugCollisionProbes.Clear();
            DebugCollisions.Clear();
        }

        if (MovementDebugEnabled)
            DebugMovementDecisions.Clear();

        var q = EntityQueryEnumerator<GridVehicleMoverComponent, VehicleComponent, TransformComponent>();

        while (q.MoveNext(out var uid, out var mover, out var vehicle, out var xform))
        {
            if (vehicle.MovementKind != VehicleMovementKind.Grid)
                continue;

            TrySyncMoverToCurrentGrid((uid, mover), centerOnTile: false, xform);

            if (xform.GridUid is not { } grid || !gridQ.TryComp(grid, out var gridComp))
                continue;

            if (_net.IsClient && !ShouldPredictVehicleMovement(vehicle))
            {
                SmoothReplicatedVehicle(uid, grid, mover, frameTime);
                continue;
            }

            var inputDir = GetMoverInput(uid, mover, vehicle, out var pushing);
            var accumulator = _movementAccumulator.GetValueOrDefault(uid) + frameTime;
            var maxAccum = MovementFixedStep * MaxFixedStepsPerFrame;
            if (accumulator > maxAccum)
                accumulator = maxAccum;

            var steps = 0;
            while (accumulator >= MovementFixedStep && steps < MaxFixedStepsPerFrame)
            {
                var currentXform = Transform(uid);
                TrySyncMoverToCurrentGrid((uid, mover), centerOnTile: false, currentXform);
                if (currentXform.GridUid is not { } currentGrid || !gridQ.TryComp(currentGrid, out var currentGridComp))
                    break;

                UpdateMovement(uid, mover, vehicle, currentGrid, currentGridComp, inputDir, pushing, MovementFixedStep);
                accumulator -= MovementFixedStep;
                steps++;
            }

            _movementAccumulator[uid] = accumulator;
        }
    }

    private bool ShouldPredictVehicleMovement(VehicleComponent vehicle)
    {
        if (!_net.IsClient)
            return true;

        if (!_timing.InPrediction)
            return false;

        return vehicle.Operator != null && vehicle.Operator == _player.LocalEntity;
    }

    private void SmoothReplicatedVehicle(EntityUid uid, EntityUid grid, GridVehicleMoverComponent mover, float frameTime)
    {
        var xform = Transform(uid);
        if (!xform.ParentUid.IsValid())
            return;

        var coords = new EntityCoordinates(grid, mover.Position);
        var target = coords.WithEntityId(xform.ParentUid, transform, EntityManager).Position;
        var current = xform.LocalPosition;
        var delta = target - current;

        if (delta.LengthSquared() >= ClientSmoothingSnapDistance * ClientSmoothingSnapDistance)
        {
            transform.SetLocalPosition(uid, target, xform);
            return;
        }

        var alpha = 1f - MathF.Exp(-ClientSmoothingRate * frameTime);
        var smoothed = Vector2.Lerp(current, target, alpha);
        transform.SetLocalPosition(uid, smoothed, xform);
    }
}
