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
using System.Collections.Generic;
using Robust.Shared.Physics;

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

    // Debug-only data used by the client overlay to visualize tiles checked for collisions.
    public static readonly List<(EntityUid grid, Vector2i tile)> DebugTestedTiles = new();

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
            var tile = GetTileForPosition(grid, gridComp, mover.Position);
            mover.CurrentTile = tile;
            mover.TargetTile = tile;
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
        var desiredRotation = new Vector2(facing.X, facing.Y).ToWorldAngle();
        mover.TargetTile = targetTile;
        mover.CurrentDirection = facing;

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

        // If the player steers to a new direction mid-segment (not just reversing),
        // drop the current commitment so we can re-orient immediately.
        if (hasInput && inputDir != mover.CurrentDirection && inputDir != -mover.CurrentDirection)
        {
            mover.IsCommittedToMove = false;
            mover.CurrentTile = GetTileForPosition(grid, gridComp, mover.Position);
            mover.TargetTile = mover.CurrentTile;
            Dirty(uid, mover);
            return;
        }

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
        var startPos = mover.Position;
        var desiredRotation = mover.CurrentDirection == Vector2i.Zero
            ? transform.GetWorldRotation(uid)
            : new Vector2(mover.CurrentDirection.X, mover.CurrentDirection.Y).ToWorldAngle();

        var reachedDesired = TryMoveAlongSegment(uid, grid, gridComp, ref mover, newPos, desiredRotation);

        // If we failed to move to the desired position (blocked), stop and wait for new input.
        if (!reachedDesired)
        {
            mover.TargetTile = mover.CurrentTile;
            mover.CurrentSpeed = 0f;
            mover.CurrentTile = GetTileForPosition(grid, gridComp, mover.Position);
            mover.TargetTile = mover.CurrentTile;
            mover.IsCommittedToMove = false;
            Dirty(uid, mover);
            return;
        }

        var remainingToTarget = (targetCenter - mover.Position).Length();

        if (remainingToTarget <= 0.01f)
        {
            mover.Position = targetCenter;
            mover.CurrentTile = mover.TargetTile;
            mover.IsCommittedToMove = false;

            var hasInput2 = inputDir != Vector2i.Zero;

            if (!hasInput2)
            {
                mover.CurrentSpeed = Math.Max(mover.CurrentSpeed - mover.Deceleration * frameTime, 0f);
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
            _ = new Vector2(facing.X, facing.Y).ToWorldAngle();
            var nextCenter = new Vector2(nextTile.X + 0.5f, nextTile.Y + 0.5f);

            mover.IsCommittedToMove = true;
            mover.TargetTile = nextTile;
            mover.CurrentDirection = facing;

            if (angleChanged)
            {
                var angle = new Vector2(facing.X, facing.Y).ToWorldAngle();
                transform.SetLocalRotation(uid, angle);
            }
        }

        SetGridPosition(uid, grid, mover.Position);
        if (mover.CurrentDirection != Vector2i.Zero &&
            Vector2.DistanceSquared(mover.Position, startPos) > 0.0001f)
        {
            var ang = new Vector2(mover.CurrentDirection.X, mover.CurrentDirection.Y).ToWorldAngle();
            transform.SetLocalRotation(uid, ang);
        }
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

    private bool CanOccupyTransform(EntityUid uid, EntityUid grid, Vector2 gridPos, Angle? overrideRotation = null)
    {
        if (!physicsQ.TryComp(uid, out var body))
            return true;

        if (!body.CanCollide)
            return true;

        if (!gridQ.TryComp(grid, out var gridComp))
            return true;

        var coords = new EntityCoordinates(grid, gridPos);
        var world = coords.ToMap(EntityManager, transform);
        var rotation = overrideRotation ?? transform.GetWorldRotation(uid);

        var tileIndices = map.TileIndicesFor(grid, gridComp, world);
        DebugTestedTiles.Add((grid, tileIndices));

        if (!fixtureQ.TryComp(uid, out var fixtures))
            return true;

        var targetTransform = new Transform(world.Position, rotation);

        if (!TryGetFixtureAabb(fixtures, targetTransform, out var aabb))
            return true;

        var mapId = world.MapId;
        var myLayer = body.CollisionLayer;
        var myMask = body.CollisionMask;

        var hits = _lookup.GetEntitiesIntersecting(mapId, aabb, LookupFlags.Dynamic | LookupFlags.Static);

        foreach (var other in hits)
        {
            if (other == uid)
                continue;

            // Ignore grids and maps; we only care about actual collidable entities.
            if (gridQ.HasComp(other) || HasComp<MapComponent>(other) || other == grid)
                continue;

            var otherXform = Transform(other);

            if (otherXform.MapID != mapId)
                continue;

            if (!physicsQ.TryComp(other, out var otherBody) || !otherBody.CanCollide)
                continue;

            var otherLayer = otherBody.CollisionLayer;
            var otherMask = otherBody.CollisionMask;

            if ((myMask & otherLayer) == 0 && (myLayer & otherMask) == 0)
                continue;

            if (!fixtureQ.TryComp(other, out var otherFixtures) || otherFixtures.FixtureCount == 0)
                continue;

            var otherTransform = physics.GetPhysicsTransform(other, otherXform);

            if (physics.TryGetNearest(uid, other, out _, out _, out var distance,
                    targetTransform, otherTransform, fixtures, otherFixtures, body, otherBody))
            {
                var skin = GetMaxShapeRadius(fixtures, body) + GetMaxShapeRadius(otherFixtures, otherBody);

                // If the gap between shapes is less than or equal to their combined radii, they would overlap.
                if (distance <= skin)
                    return false;
            }
        }

        return true;
    }

    private bool TryGetFixtureAabb(FixturesComponent fixtures, Transform transform, out Box2 aabb)
    {
        var first = true;
        aabb = default;

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            for (var i = 0; i < fixture.Shape.ChildCount; i++)
            {
                var child = fixture.Shape.ComputeAABB(transform, i);

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

    private float GetMaxShapeRadius(FixturesComponent fixtures, PhysicsComponent body)
    {
        var max = 0f;

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            if (body.Hard && !fixture.Hard)
                continue;

            // All child shapes on a fixture share the same radius.
            max = Math.Max(max, fixture.Shape.Radius);
        }

        return max;
    }

    private bool TryMoveAlongSegment(EntityUid uid, EntityUid grid, MapGridComponent gridComp, ref GridVehicleMoverComponent mover, Vector2 desiredPos, Angle overrideRotation)
    {
        var rotation = overrideRotation;
        var start = mover.Position;
        var delta = desiredPos - start;

        if (delta == Vector2.Zero)
            return true;

        var distance = delta.Length();
        var dir = delta / distance;

        // Fast path: whole step fits.
        if (CanOccupyTransform(uid, grid, desiredPos, rotation))
        {
            mover.Position = desiredPos;
            SetGridPosition(uid, grid, mover.Position);
            return true;
        }

        // Binary search along the segment to find the furthest non-overlapping point.
        var low = 0f;
        var high = distance;
        var best = start;

        for (var i = 0; i < 8; i++)
        {
            var mid = (low + high) * 0.5f;
            var pos = start + dir * mid;

            if (CanOccupyTransform(uid, grid, pos, rotation))
            {
                best = pos;
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        mover.Position = best;
        SetGridPosition(uid, grid, mover.Position);

        // Return true only if we reached (or essentially reached) the desired position.
        return (desiredPos - best).Length() <= 0.01f;
    }

    private Vector2i GetTileForPosition(EntityUid grid, MapGridComponent gridComp, Vector2 gridPos)
    {
        var coords = new EntityCoordinates(grid, gridPos);
        return map.TileIndicesFor(grid, gridComp, coords);
    }

}
