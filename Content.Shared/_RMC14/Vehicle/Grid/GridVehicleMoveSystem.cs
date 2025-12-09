using System;
using System.Numerics;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Vehicle.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Vehicle;

public sealed class GridVehicleMoverSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming timing = default!;
    [Dependency] private readonly SharedTransformSystem transform = default!;
    [Dependency] private readonly SharedMapSystem map = default!;
    [Dependency] private readonly SharedPhysicsSystem physics = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private EntityQuery<MapGridComponent> gridQ;
    private EntityQuery<PhysicsComponent> physicsQ;
    private EntityQuery<FixturesComponent> fixtureQ;

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
        var indices = map.TileIndicesFor(grid, gridComp, coords);
        ent.Comp.CurrentTile = indices;
        ent.Comp.TargetTile = indices;
        ent.Comp.Position = new Vector2(indices.X + 0.5f, indices.Y + 0.5f);
        ent.Comp.IsCommittedToMove = false;
        Dirty(uid, ent.Comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GridVehicleMoverComponent, VehicleComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var mover, out var vehicle, out var xform))
        {
            if (vehicle.MovementKind != VehicleMovementKind.Grid)
                continue;

            if (xform.GridUid is not { } grid || !gridQ.TryComp(grid, out var gridComp))
                continue;

            Vector2i inputDir = Vector2i.Zero;
            if (vehicle.Operator is { } op && TryComp<InputMoverComponent>(op, out var input))
                inputDir = GetInputDirection(input);

            UpdateMovement(uid, mover, grid, gridComp, inputDir, frameTime);
        }
    }

    private void UpdateMovement(EntityUid uid, GridVehicleMoverComponent mover, EntityUid grid,
        MapGridComponent gridComp, Vector2i inputDir, float frameTime)
    {
        var hasInput = inputDir != Vector2i.Zero;

        if (mover.IsCommittedToMove)
        {
            ContinueCommittedMove(uid, mover, grid, gridComp, inputDir, frameTime);
            return;
        }

        if (!hasInput)
        {
            mover.CurrentSpeed = 0f;
            mover.Position = new Vector2(mover.CurrentTile.X + 0.5f, mover.CurrentTile.Y + 0.5f);
            SetGridPosition(uid, grid, mover.Position);
            Dirty(uid, mover);
            return;
        }

        var facing = mover.CurrentDirection;
        var angleChanged = false;

        if (facing == Vector2i.Zero)
        {
            facing = inputDir;
            angleChanged = true;
        }

        Vector2i moveDir;

        if (inputDir == facing)
            moveDir = facing;
        else if (inputDir == -facing)
            moveDir = -facing;
        else
        {
            facing = inputDir;
            moveDir = facing;
            angleChanged = true;
        }

        mover.IsCommittedToMove = false;

        var targetTile = mover.CurrentTile + moveDir;

        if (!CanBeOnTile(uid, grid, gridComp, targetTile))
        {
            mover.CurrentSpeed = 0f;
            return;
        }

        mover.TargetTile = targetTile;
        mover.CurrentDirection = facing;

        if (angleChanged)
        {
            var angle = new Vector2(facing.X, facing.Y).ToWorldAngle();
            transform.SetLocalRotation(uid, angle);
        }

        mover.IsCommittedToMove = true;
        Dirty(uid, mover);
    }

    private void ContinueCommittedMove(EntityUid uid, GridVehicleMoverComponent mover, EntityUid grid,
        MapGridComponent gridComp, Vector2i inputDir, float frameTime)
    {
        var tileDelta = mover.TargetTile - mover.CurrentTile;
        var moveDir = new Vector2(tileDelta.X, tileDelta.Y);

        var hasInput = inputDir != Vector2i.Zero;
        var reversing = hasInput && inputDir == -mover.CurrentDirection;

        if (reversing && mover.CurrentSpeed > 0f)
            mover.CurrentSpeed = Math.Max(mover.CurrentSpeed - mover.Deceleration * frameTime * 2f, 0f);

        float targetSpeed;
        float accel;

        if (inputDir == Vector2i.Zero)
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
                targetSpeed = -mover.MaxReverseSpeed;
                accel = mover.ReverseAcceleration;
            }
        }
        else
        {
            if (mover.CurrentSpeed < 0f)
            {
                targetSpeed = 0f;
                accel = mover.Deceleration;
            }
            else
            {
                targetSpeed = mover.MaxSpeed;
                accel = mover.Acceleration;
            }
        }

        if (mover.CurrentSpeed < targetSpeed)
            mover.CurrentSpeed = Math.Min(mover.CurrentSpeed + accel * frameTime, targetSpeed);
        else if (mover.CurrentSpeed > targetSpeed)
            mover.CurrentSpeed = Math.Max(mover.CurrentSpeed - mover.Deceleration * frameTime, targetSpeed);

        if (moveDir == Vector2.Zero)
            moveDir = new Vector2(mover.CurrentDirection.X, mover.CurrentDirection.Y);

        if (moveDir != Vector2.Zero)
        {
            moveDir = Vector2.Normalize(moveDir);

            if (mover.CurrentSpeed < 0f)
                moveDir = -moveDir;
        }

        var moveAmount = mover.CurrentSpeed * frameTime;
        var newPos = mover.Position + moveDir * moveAmount;

        var targetCenter = new Vector2(mover.TargetTile.X + 0.5f, mover.TargetTile.Y + 0.5f);
        var toTarget = targetCenter - mover.Position;
        var distanceToTarget = toTarget.Length();

        if (Math.Abs(moveAmount) >= distanceToTarget)
        {
            mover.Position = targetCenter;
            mover.CurrentTile = mover.TargetTile;
            mover.IsCommittedToMove = false;

            var hasInput2 = inputDir != Vector2i.Zero;

            if (!hasInput2)
            {
                mover.CurrentSpeed = Math.Max(mover.CurrentSpeed - mover.Deceleration * frameTime, 0f);
                return;
            }

            var facing = mover.CurrentDirection;
            var angleChanged = false;

            if (facing == Vector2i.Zero)
            {
                facing = inputDir;
                angleChanged = true;
            }

            Vector2i nextMoveDir;

            if (inputDir == facing)
                nextMoveDir = facing;
            else if (inputDir == -facing)
                nextMoveDir = -facing;
            else
            {
                facing = inputDir;
                nextMoveDir = facing;
                angleChanged = true;
            }

            var nextTile = mover.CurrentTile + nextMoveDir;

            if (!CanBeOnTile(uid, grid, gridComp, nextTile))
            {
                mover.CurrentSpeed = 0f;
                return;
            }

            if (Math.Abs(mover.CurrentSpeed) < 0.01f)
                return;

            mover.IsCommittedToMove = true;
            mover.TargetTile = nextTile;
            mover.CurrentDirection = facing;

            if (angleChanged)
            {
                var angle = new Vector2(facing.X, facing.Y).ToWorldAngle();
                transform.SetLocalRotation(uid, angle);
            }
        }
        else
        {
            mover.Position = newPos;
        }

        SetGridPosition(uid, grid, mover.Position);
        physics.WakeBody(uid);
        Dirty(uid, mover);
    }

    private Vector2i GetInputDirection(InputMoverComponent input)
    {
        var buttons = input.HeldMoveButtons;
        var dir = Vector2i.Zero;

        if ((buttons & MoveButtons.Up) != 0)
            dir += new Vector2i(0, 1);
        if ((buttons & MoveButtons.Down) != 0)
            dir += new Vector2i(0, -1);
        if ((buttons & MoveButtons.Right) != 0)
            dir += new Vector2i(1, 0);
        if ((buttons & MoveButtons.Left) != 0)
            dir += new Vector2i(-1, 0);

        if (dir == Vector2i.Zero)
            return dir;

        if (dir.X != 0 && dir.Y != 0)
        {
            if (Math.Abs(dir.X) >= Math.Abs(dir.Y))
                dir = new Vector2i(Math.Sign(dir.X), 0);
            else
                dir = new Vector2i(0, Math.Sign(dir.Y));
        }
        else
            dir = new Vector2i(Math.Sign(dir.X), Math.Sign(dir.Y));

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

    private bool CanBeOnTile(EntityUid uid, EntityUid grid, MapGridComponent gridComp, Vector2i tile)
    {
        if (!physicsQ.TryComp(uid, out var body))
            return true;

        if (!body.CanCollide)
            return true;

        var coords = new EntityCoordinates(grid, new Vector2(tile.X + 0.5f, tile.Y + 0.5f));
        var world = coords.ToMap(EntityManager, transform);
        var rotation = transform.GetWorldRotation(uid);

        if (!fixtureQ.TryComp(uid, out var fixtures))
            return true;

        var aabb = new Box2(world.Position, world.Position);
        var first = true;

        foreach (var f in fixtures.Fixtures.Values)
        {
            for (var i = 0; i < f.Shape.ChildCount; i++)
            {
                var t = new Transform(world.Position, rotation);
                var child = f.Shape.ComputeAABB(t, i);
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

        if (first)
            return true;

        var mapId = transform.GetMapId(uid);

        List<EntityUid> hits = new List<EntityUid>();

        foreach (var ent in EntityManager.GetEntities())
        {
            if (ent == uid)
                continue;

            var xformOther = Transform(ent);

            if (xformOther.MapID != mapId)
                continue;

            if (!physicsQ.TryComp(ent, out var otherBody))
                continue;

            if (!otherBody.CanCollide)
                continue;

            var otherAabb = _lookup.GetWorldAABB(ent);

            if (aabb.Intersects(otherAabb))
                hits.Add(ent);
        }

        var myLayer = body.CollisionLayer;
        var myMask = body.CollisionMask;

        foreach (var other in hits)
        {
            if (!physicsQ.TryComp(other, out var otherBody))
                continue;

            var otherLayer = otherBody.CollisionLayer;
            var otherMask = otherBody.CollisionMask;

            if ((myMask & otherLayer) != 0 || (myLayer & otherMask) != 0)
                return false;
        }

        return true;
    }

}
