using System;
using System.Numerics;
using System.Collections.Generic;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Vehicle.Components;
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
    [Dependency] private readonly IGameTiming timing = default!;
    [Dependency] private readonly SharedTransformSystem transform = default!;
    [Dependency] private readonly SharedMapSystem map = default!;
    [Dependency] private readonly SharedPhysicsSystem physics = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private EntityQuery<MapGridComponent> gridQ;
    private EntityQuery<PhysicsComponent> physicsQ;
    private EntityQuery<FixturesComponent> fixtureQ;

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
        var world = xform.Coordinates.WithEntityId(grid, transform, EntityManager);
        var rotation = transform.GetWorldRotation(uid);
        var forward = rotation.ToWorldVec();
        var anchorPos = world.Position + forward * ent.Comp.FrontOffset;
        var coords = new EntityCoordinates(grid, anchorPos);
        var indices = map.TileIndicesFor(grid, gridComp, coords);
        ent.Comp.CurrentTile = indices;
        ent.Comp.TargetTile = indices;
        ent.Comp.Position = anchorPos;
        ent.Comp.IsCommittedToMove = false;
        Dirty(uid, ent.Comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        DebugCollisions.Clear();
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
            SetGridPosition(uid, grid, mover.Position, mover);
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
        var desiredRotation = mover.CurrentDirection == Vector2i.Zero
            ? transform.GetWorldRotation(uid)
            : new Vector2(mover.CurrentDirection.X, mover.CurrentDirection.Y).ToWorldAngle();
        var moveRotation = transform.GetWorldRotation(uid);
        var reachedDesired = TryMoveAlongSegment(uid, grid, gridComp, ref mover, newPos, moveRotation, desiredRotation);
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
        transform.SetLocalRotation(uid, desiredRotation);
        if (remainingToTarget <= 0.01f)
        {
            mover.Position = targetCenter;
            mover.CurrentTile = mover.TargetTile;
            mover.IsCommittedToMove = false;
            var hasInput2 = inputDir != Vector2i.Zero;
            if (!hasInput2)
            {
                mover.CurrentSpeed = Math.Max(mover.CurrentSpeed - mover.Deceleration * frameTime, 0f);
                SetGridPosition(uid, grid, mover.Position, mover);
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
                transform.SetLocalRotation(uid, new Vector2(facing.X, facing.Y).ToWorldAngle());
        }
        SetGridPosition(uid, grid, mover.Position, mover);
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

    private void SetGridPosition(EntityUid uid, EntityUid grid, Vector2 gridPos, GridVehicleMoverComponent mover)
    {
        var xform = Transform(uid);
        if (!xform.ParentUid.IsValid())
            return;
        var rotation = transform.GetWorldRotation(uid);
        var forward = mover.CurrentDirection != Vector2i.Zero
            ? Vector2.Normalize(new Vector2(mover.CurrentDirection.X, mover.CurrentDirection.Y))
            : rotation.ToWorldVec();
        var coords = new EntityCoordinates(grid, gridPos - forward * mover.FrontOffset);
        var local = coords.WithEntityId(xform.ParentUid, transform, EntityManager).Position;
        transform.SetLocalPosition(uid, local, xform);
    }

    private bool CanOccupyTransform(EntityUid uid, EntityUid grid, Vector2 gridPos, Angle? overrideRotation = null, float clearance = 0f)
    {
        if (!physicsQ.TryComp(uid, out var body))
            return true;
        if (!body.CanCollide)
            return true;
        if (!gridQ.TryComp(grid, out var gridComp))
            return true;
        var rotation = overrideRotation ?? transform.GetWorldRotation(uid);
        var forward = rotation.ToWorldVec();
        var frontOffset = 0f;
        if (TryComp(uid, out GridVehicleMoverComponent? moverComp))
            frontOffset = moverComp.FrontOffset;
        var anchorCoords = new EntityCoordinates(grid, gridPos);
        var tileIndices = map.TileIndicesFor(grid, gridComp, anchorCoords);
        DebugTestedTiles.Add((grid, tileIndices));
        var bodyCoords = new EntityCoordinates(grid, gridPos - forward * frontOffset);
        var world = bodyCoords.ToMap(EntityManager, transform);
        if (!fixtureQ.TryComp(uid, out var fixtures))
            return true;
        var targetTransform = new Transform(world.Position, rotation);
        if (!TryGetFixtureAabb(fixtures, targetTransform, out var aabb))
            return true;
        var mapId = world.MapId;
        var myLayer = body.CollisionLayer;
        var myMask = body.CollisionMask;
        var currentTransform = physics.GetPhysicsTransform(uid);
        var hits = _lookup.GetEntitiesIntersecting(mapId, aabb, LookupFlags.Dynamic | LookupFlags.Static);
        foreach (var other in hits)
        {
            if (other == uid)
                continue;
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
            if (otherBody.BodyType == BodyType.Dynamic && !otherXform.Anchored)
                continue;
            if (!fixtureQ.TryComp(other, out var otherFixtures) || otherFixtures.FixtureCount == 0)
                continue;
            var otherTransform = physics.GetPhysicsTransform(other, otherXform);
            if (physics.TryGetNearest(uid, other, out _, out _, out var distance,
                    targetTransform, otherTransform, fixtures, otherFixtures, body, otherBody))
            {
                var skin = GetMaxShapeRadius(fixtures, body) + GetMaxShapeRadius(otherFixtures, otherBody);
                if (distance < skin - clearance)
                {
                    if (!TryGetFixtureAabb(otherFixtures, otherTransform, out var otherAabb))
                        otherAabb = _lookup.GetWorldAABB(other);
                    DebugCollisions.Add(new DebugCollision(uid, other, aabb, otherAabb, distance, skin, clearance));
                    return false;
                }
            }
        }
        return true;
    }

    private bool CanSeparateFromCurrentOverlaps(EntityUid uid, EntityUid grid, Vector2 startPos, Vector2 targetPos, Angle moveRotation)
    {
        if (!physicsQ.TryComp(uid, out var body) || !fixtureQ.TryComp(uid, out var fixtures) || !gridQ.TryComp(grid, out var gridComp))
            return false;
        var startForward = moveRotation.ToWorldVec();
        var targetForward = startForward;
        var frontOffset = 0f;
        if (TryComp(uid, out GridVehicleMoverComponent? moverComp))
            frontOffset = moverComp.FrontOffset;
        var startCoords = new EntityCoordinates(grid, startPos - startForward * frontOffset);
        var targetCoords = new EntityCoordinates(grid, targetPos - targetForward * frontOffset);
        var startWorld = startCoords.ToMap(EntityManager, transform);
        var targetWorld = targetCoords.ToMap(EntityManager, transform);
        var startTransform = new Transform(startWorld.Position, moveRotation);
        var targetTransform = new Transform(targetWorld.Position, moveRotation);
        if (!TryGetFixtureAabb(fixtures, targetTransform, out var targetAabb))
            return false;
        var mapId = targetWorld.MapId;
        var myLayer = body.CollisionLayer;
        var myMask = body.CollisionMask;
        var hits = _lookup.GetEntitiesIntersecting(mapId, targetAabb, LookupFlags.Dynamic | LookupFlags.Static);
        foreach (var other in hits)
        {
            if (other == uid)
                continue;
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
            if (!physics.TryGetNearest(uid, other, out _, out _, out var targetDistance,
                    targetTransform, otherTransform, fixtures, otherFixtures, body, otherBody))
                continue;
            if (!physics.TryGetNearest(uid, other, out _, out _, out var currentDistance,
                    startTransform, otherTransform, fixtures, otherFixtures, body, otherBody))
                continue;
            var skin = GetMaxShapeRadius(fixtures, body) + GetMaxShapeRadius(otherFixtures, otherBody);
            if (targetDistance < skin && currentDistance >= skin)
                return false;
            if (targetDistance < skin && currentDistance < skin && targetDistance + PhysicsConstants.LinearSlop >= currentDistance)
                return false;
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
            max = Math.Max(max, fixture.Shape.Radius);
        }
        return max;
    }

    private bool CanRotateSweep(EntityUid uid, EntityUid grid, Vector2 gridPos, Angle fromRotation, Angle toRotation)
    {
        var steps = 10;
        for (var i = 1; i <= steps; i++)
        {
            var t = (float)i / steps;
            var angle = Angle.Lerp(fromRotation, toRotation, t);
            if (!CanOccupyTransform(uid, grid, gridPos, angle))
                return false;
        }
        return true;
    }

    private bool TryApplyRotationWithClearance(EntityUid uid, EntityUid grid, ref GridVehicleMoverComponent mover, Angle moveRotation, Angle targetRotation)
    {
        var startPos = mover.Position;
        if (CanRotateSweep(uid, grid, startPos, moveRotation, targetRotation))
        {
            transform.SetLocalRotation(uid, targetRotation);
            return true;
        }
        var maxRadius = 1.5f;
        var radiusStep = 0.15f;
        var angularSteps = 16;
        for (var r = radiusStep; r <= maxRadius + 0.001f; r += radiusStep)
        {
            for (var i = 0; i < angularSteps; i++)
            {
                var angle = (float)(Math.PI * 2 * i / angularSteps);
                var offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * r;
                var candidatePos = startPos + offset;
                if (!CanOccupyTransform(uid, grid, candidatePos, moveRotation))
                    continue;
                if (!CanRotateSweep(uid, grid, candidatePos, moveRotation, targetRotation))
                    continue;
                if (!CanSeparateFromCurrentOverlaps(uid, grid, startPos, candidatePos, moveRotation))
                    continue;
                mover.Position = candidatePos;
                SetGridPosition(uid, grid, mover.Position, mover);
                transform.SetLocalRotation(uid, targetRotation);
                return true;
            }
        }
        return false;
    }

    private bool TryMoveAlongSegment(EntityUid uid, EntityUid grid, MapGridComponent gridComp, ref GridVehicleMoverComponent mover, Vector2 desiredPos, Angle moveRotation, Angle targetRotation)
    {
        var start = mover.Position;
        var delta = desiredPos - start;
        if (delta == Vector2.Zero)
        {
            TryApplyRotationWithClearance(uid, grid, ref mover, moveRotation, targetRotation);
            return true;
        }
        var distance = delta.Length();
        var dir = delta / distance;
        if (CanOccupyTransform(uid, grid, desiredPos, moveRotation))
        {
            mover.Position = desiredPos;
            SetGridPosition(uid, grid, mover.Position, mover);
            TryApplyRotationWithClearance(uid, grid, ref mover, moveRotation, targetRotation);
            return true;
        }
        if (CanSeparateFromCurrentOverlaps(uid, grid, start, desiredPos, moveRotation))
        {
            mover.Position = desiredPos;
            SetGridPosition(uid, grid, mover.Position, mover);
            TryApplyRotationWithClearance(uid, grid, ref mover, moveRotation, targetRotation);
            return true;
        }
        var low = 0f;
        var high = distance;
        var best = start;
        var found = false;
        for (var i = 0; i < 8; i++)
        {
            var mid = (low + high) * 0.5f;
            var pos = start + dir * mid;
            if (CanOccupyTransform(uid, grid, pos, moveRotation))
            {
                best = pos;
                low = mid;
                found = true;
            }
            else
            {
                high = mid;
            }
        }
        if (!found)
            return false;
        mover.Position = best;
        SetGridPosition(uid, grid, mover.Position, mover);
        TryApplyRotationWithClearance(uid, grid, ref mover, moveRotation, targetRotation);
        return (desiredPos - best).Length() <= 0.01f;
    }

    private Vector2i GetTileForPosition(EntityUid grid, MapGridComponent gridComp, Vector2 gridPos)
    {
        var coords = new EntityCoordinates(grid, gridPos);
        return map.TileIndicesFor(grid, gridComp, coords);
    }
}
