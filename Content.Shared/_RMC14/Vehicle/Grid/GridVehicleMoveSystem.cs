using System;
using System.Numerics;
using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Foldable;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Vehicle.Components;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Entrenching;
using Content.Shared._RMC14.Xenonids;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Vehicle;

public sealed class GridVehicleMoverSystem : EntitySystem
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
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;
    [Dependency] private readonly RMCVehicleWheelSystem _wheels = default!;

    private EntityQuery<MapGridComponent> gridQ;
    private EntityQuery<PhysicsComponent> physicsQ;
    private EntityQuery<FixturesComponent> fixtureQ;

    private const float Clearance = PhysicsConstants.PolygonRadius * 0.75f;
    private const double MobCollisionDamage = 8;
    private static readonly TimeSpan MobCollisionKnockdown = TimeSpan.FromSeconds(1.5);
    private static readonly TimeSpan MobCollisionCooldown = TimeSpan.FromSeconds(0.75);
    private static readonly ProtoId<DamageTypePrototype> CollisionDamageType = "Blunt";

    public static readonly List<(EntityUid grid, Vector2i tile)> DebugTestedTiles = new();
    public static readonly List<DebugCollision> DebugCollisions = new();
    private readonly Dictionary<EntityUid, TimeSpan> _lastMobCollision = new();
    private readonly Dictionary<EntityUid, bool> _hardState = new();

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
        ent.Comp.IsCommittedToMove = false;
        _hardState[uid] = true;

        Dirty(uid, ent.Comp);
    }

    private void OnMoverShutdown(Entity<GridVehicleMoverComponent> ent, ref ComponentShutdown args)
    {
        _hardState.Remove(ent.Owner);
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

            Vector2i inputDir = Vector2i.Zero;

            if (vehicle.Operator is { } op && TryComp<InputMoverComponent>(op, out var inputComp))
                inputDir = GetInputDirection(inputComp);

            UpdateMovement(uid, mover, vehicle, grid, gridComp, inputDir, frameTime);
        }
    }

    private void UpdateMovement(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        VehicleComponent vehicle,
        EntityUid grid,
        MapGridComponent gridComp,
        Vector2i inputDir,
        float frameTime)
    {
        var canRunEvent = new VehicleCanRunEvent((uid, vehicle));
        RaiseLocalEvent(uid, ref canRunEvent);
        if (!canRunEvent.CanRun)
        {
            mover.CurrentSpeed = 0f;
            mover.IsCommittedToMove = false;
            mover.IsMoving = false;
            SetGridPosition(uid, grid, mover.Position);
            Dirty(uid, mover);
            return;
        }

        // Ensure smash slowdowns expire even if the vehicle is idle.
        GetSmashSlowdownMultiplier(mover);

        if (mover.IsCommittedToMove)
        {
            ContinueCommittedMove(uid, mover, grid, gridComp, inputDir, frameTime);
            return;
        }

        var hasInput = inputDir != Vector2i.Zero;

        if (!hasInput)
        {
            if (mover.CurrentSpeed > 0f)
                mover.CurrentSpeed = MathF.Max(0f, mover.CurrentSpeed - mover.Deceleration * frameTime);
            else if (mover.CurrentSpeed < 0f)
                mover.CurrentSpeed = MathF.Min(0f, mover.CurrentSpeed + mover.Deceleration * frameTime);

            var tile = GetTile(grid, gridComp, mover.Position);
            mover.CurrentTile = tile;
            mover.TargetTile = tile;
            mover.IsMoving = MathF.Abs(mover.CurrentSpeed) > 0.01f;
            if (mover.IsMoving)
                PlayRunningSound(uid);
            SetGridPosition(uid, grid, mover.Position);
            Dirty(uid, mover);
            return;
        }

        CommitNextTile(uid, mover, grid, gridComp, inputDir);
        SetGridPosition(uid, grid, mover.Position);
        Dirty(uid, mover);
    }

    private void ContinueCommittedMove(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        MapGridComponent gridComp,
        Vector2i inputDir,
        float frameTime)
    {
        var tileDelta = mover.TargetTile - mover.CurrentTile;
        if (tileDelta == Vector2i.Zero)
        {
            mover.IsCommittedToMove = false;
            return;
        }

        var maxSpeed = mover.MaxSpeed;
        var maxReverseSpeed = mover.MaxReverseSpeed;

        var smashMultiplier = GetSmashSlowdownMultiplier(mover);
        maxSpeed *= smashMultiplier;
        maxReverseSpeed *= smashMultiplier;

        if (TryComp<RMCVehicleOverchargeComponent>(uid, out var overcharge) && _timing.CurTime < overcharge.ActiveUntil)
        {
            maxSpeed *= overcharge.SpeedMultiplier;
            maxReverseSpeed *= overcharge.SpeedMultiplier;
        }

        var targetCenter = new Vector2(mover.TargetTile.X + 0.5f, mover.TargetTile.Y + 0.5f);
        var toTarget = targetCenter - mover.Position;
        var distToTarget = toTarget.Length();

        var hasInput = inputDir != Vector2i.Zero;
        var hasInputForSpeed = hasInput || mover.IsCommittedToMove;
        var facing = mover.CurrentDirection;
        var reversing = hasInput && facing != Vector2i.Zero && inputDir == -facing;

        float targetSpeed;
        float accel;

        if (!hasInputForSpeed)
        {
            targetSpeed = 0f;
            accel = mover.Deceleration;
        }
        else if (reversing)
        {
            if (mover.CurrentSpeed > 0f)
            {
                targetSpeed = 0f;
                accel = mover.Deceleration;
            }
            else
            {
                targetSpeed = -maxReverseSpeed;
                accel = mover.ReverseAcceleration;
            }
        }
        else
        {
            if (mover.CurrentSpeed < 0f && hasInputForSpeed)
            {
                targetSpeed = 0f;
                accel = mover.Deceleration;
            }
            else
            {
                targetSpeed = maxSpeed;
                accel = mover.Acceleration;
            }
        }

        if (mover.CurrentSpeed < targetSpeed)
            mover.CurrentSpeed = MathF.Min(mover.CurrentSpeed + accel * frameTime, targetSpeed);
        else if (mover.CurrentSpeed > targetSpeed)
            mover.CurrentSpeed = MathF.Max(mover.CurrentSpeed - mover.Deceleration * frameTime, targetSpeed);

        var speedMag = MathF.Abs(mover.CurrentSpeed);
        var moveAmount = speedMag * frameTime;

        if (distToTarget <= 0.0001f || moveAmount >= distToTarget)
        {
            mover.Position = targetCenter;
            mover.CurrentTile = mover.TargetTile;

            if (!hasInput || MathF.Abs(mover.CurrentSpeed) <= 0.0001f)
            {
                mover.IsCommittedToMove = false;
                mover.CurrentSpeed = 0f;
            }
            else
            {
                CommitNextTile(uid, mover, grid, gridComp, inputDir);
            }
        }
        else
        {
            var dir = toTarget / distToTarget;
            mover.Position += dir * moveAmount;
        }
        mover.IsMoving = MathF.Abs(mover.CurrentSpeed) > 0.01f;
        if (mover.IsMoving)
            PlayRunningSound(uid);

        SetGridPosition(uid, grid, mover.Position);
        physics.WakeBody(uid);
        Dirty(uid, mover);
    }

    private void CommitNextTile(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        MapGridComponent gridComp,
        Vector2i inputDir)
    {
        if (inputDir == Vector2i.Zero)
        {
            mover.IsCommittedToMove = false;
            return;
        }

        var facing = mover.CurrentDirection;
        var hadFacing = facing != Vector2i.Zero;

        if (!hadFacing)
            facing = inputDir;

        var reversing = hadFacing && inputDir == -facing;

        if (!reversing)
            facing = inputDir;

        Vector2i moveDir;

        if (reversing)
            moveDir = -facing;
        else
            moveDir = facing;

        var targetTile = mover.CurrentTile + moveDir;
        var targetCenter = new Vector2(targetTile.X + 0.5f, targetTile.Y + 0.5f);
        var desiredRot = new Vector2(facing.X, facing.Y).ToWorldAngle();

        if (!CanOccupyTransform(uid, mover, grid, targetCenter, desiredRot, Clearance))
        {
            // Try to at least rotate in place if there's room on the current tile.
            if (!reversing)
            {
                var currentCenter = new Vector2(mover.CurrentTile.X + 0.5f, mover.CurrentTile.Y + 0.5f);
                if (CanOccupyTransform(uid, mover, grid, currentCenter, desiredRot, Clearance))
                {
                    mover.CurrentDirection = facing;
                    transform.SetLocalRotation(uid, desiredRot);
                }
            }

            mover.TargetTile = mover.CurrentTile;
            mover.IsCommittedToMove = false;
            mover.CurrentSpeed = 0f;
            return;
        }

        if (!reversing)
        {
            mover.CurrentDirection = facing;
            transform.SetLocalRotation(uid, desiredRot);
        }

        mover.TargetTile = targetTile;
        mover.IsCommittedToMove = true;
    }

    private Vector2i GetInputDirection(InputMoverComponent input)
    {
        var buttons = input.HeldMoveButtons;
        var dir = Vector2i.Zero;

        if ((buttons & MoveButtons.Up) != 0) dir += new Vector2i(0, 1);
        if ((buttons & MoveButtons.Down) != 0) dir += new Vector2i(0, -1);
        if ((buttons & MoveButtons.Right) != 0) dir += new Vector2i(1, 0);
        if ((buttons & MoveButtons.Left) != 0) dir += new Vector2i(-1, 0);

        if (dir == Vector2i.Zero)
            return dir;

        if (dir.X != 0 && dir.Y != 0)
        {
            if (Math.Abs(dir.X) >= Math.Abs(dir.Y))
                dir = new Vector2i(Math.Sign(dir.X), 0);
            else
                dir = new Vector2i(0, Math.Sign(dir.Y));
        }

        return dir;
    }

    private void SetGridPosition(EntityUid uid, EntityUid grid, Vector2 gridPos)
    {
        var xform = Transform(uid);
        if (!xform.ParentUid.IsValid())
            return;

        var coords = new EntityCoordinates(grid, gridPos);
        var local = coords.WithEntityId(xform.ParentUid, transform, EntityManager).Position;

        transform.SetLocalPosition(uid, local, xform);
    }

    private bool CanOccupyTransform(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        Vector2 gridPos,
        Angle? overrideRotation,
        float clearance)
    {
        if (!physicsQ.TryComp(uid, out var body) || !fixtureQ.TryComp(uid, out var fixtures))
            return true;

        EntityUid? operatorUid = null;
        if (TryComp<VehicleComponent>(uid, out var vehicleComp))
            operatorUid = vehicleComp.Operator;

        if (!body.CanCollide)
            return true;

        if (!gridQ.TryComp(grid, out var gridComp))
            return true;

        var coords = new EntityCoordinates(grid, gridPos);
        var world = coords.ToMap(EntityManager, transform);

        var tileIndices = map.TileIndicesFor(grid, gridComp, coords);
        DebugTestedTiles.Add((grid, tileIndices));

        var rotation = overrideRotation ?? transform.GetWorldRotation(uid);
        var tx = new Transform(world.Position, rotation);

        var wheelDamage = _net.IsClient ? 0f : GetWheelCollisionDamage(uid, mover);

        if (!TryGetFixtureAabb(fixtures, tx, out var aabb))
            return true;

        var hits = lookup.GetEntitiesIntersecting(world.MapId, aabb, LookupFlags.Dynamic | LookupFlags.Static);
        var playedCollisionSound = false;
        var blocked = false;
        var mobHits = new Dictionary<EntityUid, Box2>();

        foreach (var other in hits)
        {
            if (other == uid)
                continue;

            if (!physicsQ.TryComp(other, out var otherBody) || !otherBody.CanCollide)
                continue;

            if (!fixtureQ.TryComp(other, out var otherFixtures))
                continue;

            var otherXform = Transform(other);
            var otherTx = physics.GetPhysicsTransform(other, otherXform);

            if (!TryGetFixtureAabb(otherFixtures, otherTx, out var otherAabb))
                continue;

            if (!aabb.Intersects(otherAabb))
                continue;

            var hardCollidable = physics.IsHardCollidable((uid, fixtures, body), (other, otherFixtures, otherBody));
            var skipBlocking = false;

            if (!otherXform.Anchored && HasComp<ItemComponent>(other))
                continue;

            var isBarricade = HasComp<BarricadeComponent>(other);
            var hasDoor = TryComp(other, out DoorComponent? door);
            var isFoldable = HasComp<FoldableComponent>(other);
            var isMob = TryComp(other, out MobStateComponent? mob);
            var isXeno = HasComp<XenoComponent>(other);
            var isVehicle = HasComp<VehicleComponent>(other);
            var isLooseDynamic =
                !otherXform.Anchored &&
                otherBody.BodyType != BodyType.Static &&
                !isMob &&
                !isBarricade &&
                !isFoldable &&
                !isVehicle &&
                !HasComp<RMCVehicleSmashableComponent>(other);

            if (isLooseDynamic)
                continue;

            if (isXeno)
            {
                var blocksXeno = ShouldBlockXeno(mover, other);

                if (blocksXeno)
                {
                    PlayMobCollisionSound(uid, ref playedCollisionSound);
                    ApplyWheelCollisionDamage(uid, mover, wheelDamage);
                    DebugCollisions.Add(new DebugCollision(uid, other, aabb, otherAabb, 0f, 0f, clearance, world.MapId));
                    return false;
                }

                PlayMobCollisionSound(uid, ref playedCollisionSound);
                if (!_net.IsClient)
                    PushMobOutOfVehicle(uid, other, aabb, otherAabb);

                continue;
            }

            if (hasDoor && !_net.IsClient)
            {
                _door.TryOpen(other, door, operatorUid);
                if (isBarricade)
                    _door.OnPartialOpen(other, door);
            }

            if (isMob && !isXeno)
                skipBlocking = true;

            if (isBarricade && (hasDoor || isFoldable))
                skipBlocking = true;

            if (isFoldable && !hardCollidable)
                continue;

            if (TrySmash(other, uid, ref playedCollisionSound))
                continue;

            var isBlocked = !skipBlocking && hardCollidable;
            if (isBlocked)
            {
                PlayCollisionSound(uid, ref playedCollisionSound);
                ApplyWheelCollisionDamage(uid, mover, wheelDamage);
                DebugCollisions.Add(new DebugCollision(uid, other, aabb, otherAabb, 0f, 0f, clearance, world.MapId));
                blocked = true;
                break;
            }

            if (!_net.IsClient && isMob && mob != null)
                mobHits[other] = otherAabb;
        }

        if (blocked)
            return false;

        if (!_net.IsClient)
        {
            foreach (var (mobUid, mobAabb) in mobHits)
            {
                if (!TryComp(mobUid, out MobStateComponent? mob))
                    continue;

                HandleMobCollision(uid, mobUid, mob, ref playedCollisionSound);
                if (!HasComp<XenoComponent>(mobUid))
                    PushMobOutOfVehicle(uid, mobUid, aabb, mobAabb);
            }
        }

        return true;
    }

    private void ApplyWheelCollisionDamage(EntityUid vehicle, GridVehicleMoverComponent mover, float damage)
    {
        if (_net.IsClient || damage <= 0f)
            return;

        _wheels.DamageWheels(vehicle, damage);
    }

    private float GetWheelCollisionDamage(EntityUid vehicle, GridVehicleMoverComponent mover)
    {
        if (!TryComp(vehicle, out RMCVehicleWheelSlotsComponent? wheels))
            return 0f;

        var speedMag = MathF.Abs(mover.CurrentSpeed);
        if (speedMag <= 0f)
            return 0f;

        var damage = speedMag * wheels.CollisionDamagePerSpeed;

        if (wheels.MinCollisionDamage > 0f)
            damage = MathF.Max(wheels.MinCollisionDamage, damage);

        return damage;
    }

    private bool ShouldBlockXeno(GridVehicleMoverComponent mover, EntityUid xeno)
    {
        if (mover.XenoBlockMinimumSize is not { } minSize)
            return true;

        if (!_size.TryGetSize(xeno, out var size))
            return true;

        return size >= minSize;
    }

    private bool TryGetFixtureAabb(FixturesComponent fixtures, Transform transformData, out Box2 aabb)
    {
        var first = true;
        aabb = default;

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            if (!fixture.Hard)
                continue;

            for (var i = 0; i < fixture.Shape.ChildCount; i++)
            {
                var child = fixture.Shape.ComputeAABB(transformData, i);

                if (first)
                {
                    aabb = child;
                    first = false;
                }
                else
                {
                    aabb = aabb.Union(child);
                }
            }
        }

        return !first;
    }

    private Vector2i GetTile(EntityUid grid, MapGridComponent gridComp, Vector2 pos)
    {
        var coords = new EntityCoordinates(grid, pos);
        return map.TileIndicesFor(grid, gridComp, coords);
    }

    private void PlayRunningSound(EntityUid uid)
    {
        if (!TryComp<RMCVehicleSoundComponent>(uid, out var sound))
            return;

        if (sound.RunningSound == null)
            return;

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        if (sound.NextRunningSound > now)
            return;

        _audio.PlayPvs(sound.RunningSound, uid);
        sound.NextRunningSound = now + TimeSpan.FromSeconds(sound.RunningSoundCooldown);
        Dirty(uid, sound);
    }

    private bool TrySmash(EntityUid target, EntityUid vehicle, ref bool playedCollisionSound)
    {
        if (!TryComp(target, out RMCVehicleSmashableComponent? smashable))
            return false;

        PlayCollisionSound(vehicle, ref playedCollisionSound);

        if (TryComp(vehicle, out GridVehicleMoverComponent? mover))
            ApplySmashSlowdown(vehicle, mover, smashable);

        if (!_net.IsClient)
        {
            if (smashable.SmashSound != null)
                _audio.PlayPvs(smashable.SmashSound, vehicle);

            if (smashable.DeleteOnHit && !TerminatingOrDeleted(target))
                Del(target);
        }

        return true;
    }

    private float GetSmashSlowdownMultiplier(GridVehicleMoverComponent mover)
    {
        if (mover.SmashSlowdownMultiplier >= 1f && mover.SmashSlowdownUntil == TimeSpan.Zero)
            return 1f;

        var now = _timing.CurTime;
        if (mover.SmashSlowdownUntil != TimeSpan.Zero && now >= mover.SmashSlowdownUntil)
        {
            mover.SmashSlowdownMultiplier = 1f;
            mover.SmashSlowdownUntil = TimeSpan.Zero;
            return 1f;
        }

        return Math.Clamp(mover.SmashSlowdownMultiplier, 0f, 1f);
    }

    private void ApplySmashSlowdown(EntityUid vehicle, GridVehicleMoverComponent mover, RMCVehicleSmashableComponent smashable)
    {
        if (smashable.SlowdownDuration <= 0f || smashable.SlowdownMultiplier >= 1f)
            return;

        var now = _timing.CurTime;
        mover.SmashSlowdownMultiplier = MathF.Min(mover.SmashSlowdownMultiplier, smashable.SlowdownMultiplier);
        var until = now + TimeSpan.FromSeconds(smashable.SlowdownDuration);
        if (until > mover.SmashSlowdownUntil)
            mover.SmashSlowdownUntil = until;
        mover.CurrentSpeed *= smashable.SlowdownMultiplier;
    }

    private void PlayCollisionSound(EntityUid uid, ref bool played)
    {
        if (played)
            return;

        if (!TryComp<RMCVehicleSoundComponent>(uid, out var sound))
            return;

        if (sound.CollisionSound == null)
            return;

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        if (sound.NextCollisionSound > now)
            return;

        _audio.PlayPvs(sound.CollisionSound, uid);
        sound.NextCollisionSound = now + TimeSpan.FromSeconds(sound.CollisionSoundCooldown);
        Dirty(uid, sound);
        played = true;
    }

    private void PlayMobCollisionSound(EntityUid uid, ref bool played)
    {
        if (played)
            return;

        if (!TryComp<RMCVehicleSoundComponent>(uid, out var sound))
            return;

        var mobSound = sound.MobCollisionSound ?? sound.CollisionSound;
        if (mobSound == null)
            return;

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        if (sound.NextCollisionSound > now)
            return;

        _audio.PlayPvs(mobSound, uid);
        sound.NextCollisionSound = now + TimeSpan.FromSeconds(sound.CollisionSoundCooldown);
        Dirty(uid, sound);
        played = true;
    }

    private void HandleMobCollision(EntityUid vehicle, EntityUid target, MobStateComponent mobState, ref bool playedCollisionSound)
    {
        if (_net.IsClient || _mobState.IsDead(target, mobState))
            return;

        var now = _timing.CurTime;
        if (_lastMobCollision.TryGetValue(target, out var last) && now < last + MobCollisionCooldown)
            return;

        _lastMobCollision[target] = now;

        PlayMobCollisionSound(vehicle, ref playedCollisionSound);

        var damage = new DamageSpecifier
        {
            DamageDict =
            {
                [CollisionDamageType] = MobCollisionDamage,
            },
        };

        _damageable.TryChangeDamage(target, damage);

        if (HasComp<XenoComponent>(target))
            return;

        _stun.TryKnockdown(target, MobCollisionKnockdown, true);

        if (physicsQ.TryComp(target, out var targetBody))
        {
            physics.SetLinearVelocity(target, Vector2.Zero, body: targetBody);
            physics.SetAngularVelocity(target, 0f, body: targetBody);
        }
    }


    private void PushMobOutOfVehicle(EntityUid vehicle, EntityUid mob, Box2 vehicleAabb, Box2 mobAabb)
    {
        var xform = Transform(mob);
        if (xform.Anchored)
            return;

        var vehicleHalf = vehicleAabb.Size / 2f;
        var mobHalf = mobAabb.Size / 2f;

        var vehicleCenter = vehicleAabb.Center;
        var mobCenter = mobAabb.Center;

        var diff = mobCenter - vehicleCenter;
        var overlapX = vehicleHalf.X + mobHalf.X - Math.Abs(diff.X);
        var overlapY = vehicleHalf.Y + mobHalf.Y - Math.Abs(diff.Y);

        if (overlapX <= 0f || overlapY <= 0f)
            return;

        var push = overlapX < overlapY
            ? new Vector2(Math.Sign(diff.X == 0f ? 1f : diff.X) * overlapX, 0f)
            : new Vector2(0f, Math.Sign(diff.Y == 0f ? 1f : diff.Y) * overlapY);

        var pushMultiplier = HasComp<XenoComponent>(mob) ? 2.25f : 1.5f;
        push *= pushMultiplier;

        if (IsPushBlocked(vehicle, mob, mobAabb, push))
            return;

        var newWorldPosition = transform.GetWorldPosition(mob) + push;
        transform.SetWorldPosition(mob, newWorldPosition);

        if (physicsQ.TryComp(mob, out var mobBody))
        {
            physics.SetLinearVelocity(mob, Vector2.Zero, body: mobBody);
            physics.SetAngularVelocity(mob, 0f, body: mobBody);
        }
    }

    private bool IsPushBlocked(EntityUid vehicle, EntityUid mob, Box2 mobAabb, Vector2 push)
    {
        if (push == Vector2.Zero)
            return false;

        var xform = Transform(mob);
        var mapId = xform.MapID;
        if (mapId == MapId.Nullspace)
            return false;

        if (!physicsQ.TryComp(mob, out var mobBody) || !fixtureQ.TryComp(mob, out var mobFixtures))
            return false;

        var shifted = mobAabb.Translated(push);
        var hits = lookup.GetEntitiesIntersecting(mapId, shifted, LookupFlags.Dynamic | LookupFlags.Static);
        foreach (var other in hits)
        {
            if (other == mob || other == vehicle)
                continue;

            if (!physicsQ.TryComp(other, out var otherBody) || !otherBody.CanCollide)
                continue;

            var otherXform = Transform(other);
            if (otherXform.Anchored || otherBody.BodyType == BodyType.Static)
                return true;

            if (!fixtureQ.TryComp(other, out var otherFixtures))
                continue;

            if (physics.IsHardCollidable((mob, mobFixtures, mobBody), (other, otherFixtures, otherBody)))
                return true;
        }

        return false;
    }
}
