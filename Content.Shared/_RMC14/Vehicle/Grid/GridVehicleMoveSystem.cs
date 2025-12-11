using System;
using System.Numerics;
using System.Collections.Generic;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Vehicle.Components;
using Content.Shared._RMC14.Vehicle;
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
    [Dependency] private readonly INetManager _net = default!;

    private EntityQuery<MapGridComponent> gridQ;
    private EntityQuery<PhysicsComponent> physicsQ;
    private EntityQuery<FixturesComponent> fixtureQ;

    private const float Clearance = PhysicsConstants.PolygonRadius * 0.75f;

    public static readonly List<(EntityUid grid, Vector2i tile)> DebugTestedTiles = new();
    public static readonly List<DebugCollision> DebugCollisions = new();

    public readonly record struct DebugCollision(
        EntityUid Tested,
        EntityUid Blocker,
        Box2 TestedAabb,
        Box2 BlockerAabb,
        float Distance,
        float Skin,
        float Clearance);

    public override void Initialize()
    {
        base.Initialize();
        gridQ = GetEntityQuery<MapGridComponent>();
        physicsQ = GetEntityQuery<PhysicsComponent>();
        fixtureQ = GetEntityQuery<FixturesComponent>();
        SubscribeLocalEvent<GridVehicleMoverComponent, ComponentStartup>(OnMoverStartup);
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

        Dirty(uid, ent.Comp);
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

            UpdateMovement(uid, mover, grid, gridComp, inputDir, frameTime);
        }
    }

    private void UpdateMovement(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        MapGridComponent gridComp,
        Vector2i inputDir,
        float frameTime)
    {
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

        if (!CanOccupyTransform(uid, grid, targetCenter, desiredRot, Clearance))
        {
            if (!reversing && !hadFacing)
            {
                mover.CurrentDirection = facing;
                transform.SetLocalRotation(uid, desiredRot);
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
        EntityUid grid,
        Vector2 gridPos,
        Angle? overrideRotation,
        float clearance)
    {
        if (!physicsQ.TryComp(uid, out var body) || !fixtureQ.TryComp(uid, out var fixtures))
            return true;

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

        if (!TryGetFixtureAabb(fixtures, tx, out var aabb))
            return true;

        var hits = lookup.GetEntitiesIntersecting(world.MapId, aabb, LookupFlags.Dynamic | LookupFlags.Static);
        var playedCollisionSound = false;

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

            if (aabb.Intersects(otherAabb))
            {
                if (TrySmash(other, uid, ref playedCollisionSound))
                    continue;

                PlayCollisionSound(uid, ref playedCollisionSound);
                DebugCollisions.Add(new DebugCollision(uid, other, aabb, otherAabb, 0f, 0f, clearance));
                return false;
            }
        }

        return true;
    }

    private bool TryGetFixtureAabb(FixturesComponent fixtures, Transform transformData, out Box2 aabb)
    {
        var first = true;
        aabb = default;

        foreach (var fixture in fixtures.Fixtures.Values)
        {
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
        if (!HasComp<RMCVehicleSmashableComponent>(target))
            return false;

        PlayCollisionSound(vehicle, ref playedCollisionSound);

        if (TerminatingOrDeleted(target))
            return true;

        Del(target);
        return true;
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
}
