using System;
using System.Numerics;
using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Doors.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Vehicle.Components;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Stun;
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
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly RMCVehicleWheelSystem _wheels = default!;

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
    public static readonly List<DebugCollision> DebugCollisions = new();
    private readonly Dictionary<EntityUid, TimeSpan> _lastMobCollision = new();
    private readonly Dictionary<EntityUid, bool> _hardState = new();
    private readonly Dictionary<EntityUid, bool> _lastMobPushAxis = new();
    private readonly Dictionary<EntityUid, float> _movementAccumulator = new();

    private enum VehicleCollisionClass : byte
    {
        Ignore = 0,
        SoftMob = 1,
        Breakable = 2,
        Hard = 3,
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

    public override void Initialize()
    {
        base.Initialize();
        gridQ = GetEntityQuery<MapGridComponent>();
        physicsQ = GetEntityQuery<PhysicsComponent>();
        fixtureQ = GetEntityQuery<FixturesComponent>();
        SubscribeLocalEvent<GridVehicleMoverComponent, ComponentStartup>(OnMoverStartup);
        SubscribeLocalEvent<GridVehicleMoverComponent, ComponentShutdown>(OnMoverShutdown);
        SubscribeLocalEvent<GridVehicleMoverComponent, VehicleCanRunEvent>(OnMoverCanRun);
        SubscribeLocalEvent<GridVehicleMoverComponent, PreventCollideEvent>(OnMoverPreventCollide);
    }

    private void OnMoverStartup(Entity<GridVehicleMoverComponent> ent, ref ComponentStartup args)
    {
        var uid = ent.Owner;
        var xform = Transform(uid);

        if (xform.GridUid is not { } grid || !gridQ.TryComp(grid, out var gridComp))
            return;

        var coords = xform.Coordinates.WithEntityId(grid, transform, EntityManager);
        var tile = map.TileIndicesFor(grid, gridComp, coords);

        ent.Comp.CurrentTile = tile;
        ent.Comp.TargetTile = tile;
        ent.Comp.Position = new Vector2(tile.X + 0.5f, tile.Y + 0.5f);
        ent.Comp.CurrentSpeed = 0f;
        ent.Comp.NextPushTime = TimeSpan.Zero;
        ent.Comp.NextTurnTime = TimeSpan.Zero;
        ent.Comp.InPlaceTurnBlockUntil = TimeSpan.Zero;
        ent.Comp.IsCommittedToMove = false;
        ent.Comp.IsPushMove = false;
        _hardState[uid] = true;
        _movementAccumulator[uid] = 0f;

        Dirty(uid, ent.Comp);
    }

    private void OnMoverShutdown(Entity<GridVehicleMoverComponent> ent, ref ComponentShutdown args)
    {
        _hardState.Remove(ent.Owner);
        _movementAccumulator.Remove(ent.Owner);
    }

    private void OnMoverCanRun(Entity<GridVehicleMoverComponent> ent, ref VehicleCanRunEvent args)
    {
        if (!args.CanRun)
            return;

        if (!TryComp(ent.Owner, out VehicleComponent? vehicle) || vehicle.Operator is not { } operatorUid)
            return;

        if (!HasComp<XenoComponent>(operatorUid))
            return;

        if (!CanXenoMoveVehicle(ent.Comp, operatorUid))
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

        if (args.OtherBody.BodyType != BodyType.Static)
            return;

        if ((args.OtherFixture.CollisionLayer & GridVehicleStaticBlockerMask) == 0)
            return;

        args.Cancelled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        DebugTestedTiles.Clear();
        DebugCollisions.Clear();

        var q = EntityQueryEnumerator<GridVehicleMoverComponent, VehicleComponent, TransformComponent>();

        while (q.MoveNext(out var uid, out var mover, out var vehicle, out var xform))
        {
            if (vehicle.MovementKind != VehicleMovementKind.Grid)
                continue;

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
                UpdateMovement(uid, mover, vehicle, grid, gridComp, inputDir, pushing, MovementFixedStep);
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
