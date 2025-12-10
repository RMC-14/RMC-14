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
using Robust.Shared.Physics.Dynamics;

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
    private bool _clearedCollisionsThisTick;

    private const float TileCollisionClearance = PhysicsConstants.PolygonRadius * 0.75f;

    public override void Initialize()
    {
        base.Initialize();
        gridQ = GetEntityQuery<MapGridComponent>();
        physicsQ = GetEntityQuery<PhysicsComponent>();
        fixtureQ = GetEntityQuery<FixturesComponent>();
        SubscribeLocalEvent<GridVehicleMoverComponent, ComponentStartup>(OnMoverStartup);
    }

    //DEBUG STUFF
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

        _clearedCollisionsThisTick = false;

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
            ContinueCommittedMove(uid, mover, grid, gridComp, frameTime);
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

        var targetCenter = new Vector2(targetTile.X + 0.5f, targetTile.Y + 0.5f);
        if (!CanOccupyTransform(uid, grid, targetCenter, desiredRotation, TileCollisionClearance))
        {
            mover.TargetTile = mover.CurrentTile;
            mover.CurrentSpeed = 0f;
            mover.IsCommittedToMove = false;

            if (angleChanged && CanOccupyTransform(uid, grid, mover.Position, desiredRotation, TileCollisionClearance))
            {
                mover.CurrentDirection = facing;
                transform.SetLocalRotation(uid, desiredRotation);
            }

            Dirty(uid, mover);
            return;
        }

        mover.TargetTile = targetTile;
        mover.CurrentDirection = facing;
        mover.CurrentSpeed = mover.MaxSpeed;

        mover.IsCommittedToMove = true;
        Dirty(uid, mover);
    }

    private void ContinueCommittedMove(EntityUid uid, GridVehicleMoverComponent mover, EntityUid grid,
        MapGridComponent gridComp, float frameTime)
    {
        var tileDelta = mover.TargetTile - mover.CurrentTile;
        var moveDir = new Vector2(tileDelta.X, tileDelta.Y);
        if (moveDir == Vector2.Zero)
            return;

        moveDir = Vector2.Normalize(moveDir);
        var moveAmount = mover.MaxSpeed * frameTime;
        var targetCenter = new Vector2(mover.TargetTile.X + 0.5f, mover.TargetTile.Y + 0.5f);
        var toTarget = targetCenter - mover.Position;
        var distanceToTarget = toTarget.Length();
        var step = moveDir * moveAmount;

        if (moveAmount >= distanceToTarget)
        {
            mover.Position = targetCenter;
            mover.CurrentTile = mover.TargetTile;
            mover.IsCommittedToMove = false;
            mover.CurrentSpeed = 0f;

            var angle = new Vector2(mover.CurrentDirection.X, mover.CurrentDirection.Y).ToWorldAngle();
            transform.SetLocalRotation(uid, angle);
        }
        else
        {
            mover.Position += step;
            mover.CurrentSpeed = mover.MaxSpeed;
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

    private bool CanOccupyTransform(EntityUid uid, EntityUid grid, Vector2 gridPos, Angle? overrideRotation = null, float clearance = 0f)
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
            {
                var myMass = body.Mass;
                var otherMass = otherBody.Mass;

                if (myMass <= 0 || otherMass <= 0 || otherMass <= myMass * 0.25f)
                    continue;
            }

            if (!fixtureQ.TryComp(other, out var otherFixtures) || otherFixtures.FixtureCount == 0)
                continue;

            var otherTransform = physics.GetPhysicsTransform(other, otherXform);

            if (physics.TryGetNearest(uid, other, out _, out _, out var distance,
                    targetTransform, otherTransform, fixtures, otherFixtures, body, otherBody))
            {
                var skin = GetMaxShapeRadius(fixtures, body) + GetMaxShapeRadius(otherFixtures, otherBody);

                if (distance <= skin - clearance)
                {
                    if (!_clearedCollisionsThisTick)
                    {
                        DebugCollisions.Clear();
                        _clearedCollisionsThisTick = true;
                    }

                    if (!TryGetFixtureAabb(otherFixtures, otherTransform, out var otherAabb))
                        otherAabb = _lookup.GetWorldAABB(other);

                    DebugCollisions.Add(new DebugCollision(uid, other, aabb, otherAabb, distance, skin, clearance));
                    return false;
                }
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

    private bool TryMoveAlongSegment(EntityUid uid, EntityUid grid, MapGridComponent gridComp, ref GridVehicleMoverComponent mover, Vector2 desiredPos, Angle moveRotation)
    {
        var rotation = moveRotation;
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

    private void TryRotateWithBackoff(EntityUid uid, EntityUid grid, MapGridComponent gridComp, ref GridVehicleMoverComponent mover, Angle desiredRotation, Vector2 startPos)
    {
        const float rotationClearance = PhysicsConstants.PolygonRadius * 0.5f;
        var currentPos = mover.Position;

        // Try rotating in place with a small clearance.
        if (CanOccupyTransform(uid, grid, currentPos, desiredRotation, rotationClearance))
        {
            transform.SetLocalRotation(uid, desiredRotation);
            return;
        }

        // Binary search back along the last segment to find the closest point where we can rotate.
        var dir = startPos - currentPos;
        var low = 0f;
        var high = 1f;
        Vector2? best = null;

        for (var i = 0; i < 8; i++)
        {
            var t = (low + high) * 0.5f;
            var pos = currentPos + dir * t;

            if (CanOccupyTransform(uid, grid, pos, desiredRotation, rotationClearance))
            {
                best = pos;
                high = t; // closer to current
            }
            else
            {
                low = t;
            }
        }

        if (best != null)
        {
            mover.Position = best.Value;
            SetGridPosition(uid, grid, mover.Position);
            mover.CurrentTile = GetTileForPosition(grid, gridComp, mover.Position);
            mover.TargetTile = mover.CurrentTile;
            transform.SetLocalRotation(uid, desiredRotation);
        }
    }

    private Vector2i GetTileForPosition(EntityUid grid, MapGridComponent gridComp, Vector2 gridPos)
    {
        var coords = new EntityCoordinates(grid, gridPos);
        return map.TileIndicesFor(grid, gridComp, coords);
    }

}
