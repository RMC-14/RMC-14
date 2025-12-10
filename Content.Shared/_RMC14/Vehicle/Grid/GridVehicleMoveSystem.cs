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
    [Dependency] private readonly EntityLookupSystem lookup = default!;

    private EntityQuery<MapGridComponent> gridQ;
    private EntityQuery<PhysicsComponent> physicsQ;
    private EntityQuery<FixturesComponent> fixtureQ;

    private const float Clearance = PhysicsConstants.PolygonRadius * 0.75f;

    // -----------------------------
    // ✔ Debug restore
    // -----------------------------
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
        ent.Comp.IsCommittedToMove = false;

        Dirty(uid, ent.Comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        DebugTestedTiles.Clear();
        DebugCollisions.Clear();

        var query = EntityQueryEnumerator<GridVehicleMoverComponent, VehicleComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var mover, out var vehicle, out var xform))
        {
            if (vehicle.MovementKind != VehicleMovementKind.Grid)
                continue;

            if (xform.GridUid is not { } grid || !gridQ.TryComp(grid, out var gridComp))
                continue;

            Vector2i input = Vector2i.Zero;

            if (vehicle.Operator is { } op && TryComp<InputMoverComponent>(op, out var inputComp))
                input = GetInputDirection(inputComp);

            UpdateMovement(uid, mover, grid, gridComp, input, frameTime);
        }
    }

    private void UpdateMovement(EntityUid uid, GridVehicleMoverComponent mover, EntityUid grid,
        MapGridComponent gridComp, Vector2i inputDir, float frameTime)
    {
        bool hasInput = inputDir != Vector2i.Zero;

        if (mover.IsCommittedToMove)
        {
            ContinueCommittedMove(uid, mover, grid, gridComp, frameTime);
            return;
        }

        if (!hasInput)
        {
            mover.CurrentSpeed = 0f;
            var tile = GetTile(grid, gridComp, mover.Position);
            mover.CurrentTile = tile;
            mover.TargetTile = tile;
            SetGridPosition(uid, grid, mover.Position);
            Dirty(uid, mover);
            return;
        }

        var facing = mover.CurrentDirection;
        bool angleChanged = false;

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

        var targetTile = mover.CurrentTile + moveDir;
        var targetCenter = new Vector2(targetTile.X + 0.5f, targetTile.Y + 0.5f);
        var desiredRot = new Vector2(facing.X, facing.Y).ToWorldAngle();

        if (!CanOccupyTransform(uid, grid, targetCenter, desiredRot, Clearance))
        {
            mover.TargetTile = mover.CurrentTile;
            mover.CurrentSpeed = 0f;

            if (angleChanged && CanOccupyTransform(uid, grid, mover.Position, desiredRot, Clearance))
            {
                mover.CurrentDirection = facing;
                transform.SetLocalRotation(uid, desiredRot);
            }

            mover.IsCommittedToMove = false;
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
        var deltaTile = mover.TargetTile - mover.CurrentTile;
        if (deltaTile == Vector2i.Zero)
            return;

        var moveDir = Vector2.Normalize(new Vector2(deltaTile.X, deltaTile.Y));
        var step = moveDir * (mover.MaxSpeed * frameTime);

        var targetCenter = new Vector2(mover.TargetTile.X + 0.5f, mover.TargetTile.Y + 0.5f);
        var remaining = targetCenter - mover.Position;

        if (step.Length() >= remaining.Length())
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

        if ((buttons & MoveButtons.Up) != 0) dir += new Vector2i(0, 1);
        if ((buttons & MoveButtons.Down) != 0) dir += new Vector2i(0, -1);
        if ((buttons & MoveButtons.Right) != 0) dir += new Vector2i(1, 0);
        if ((buttons & MoveButtons.Left) != 0) dir += new Vector2i(-1, 0);

        if (dir == Vector2i.Zero)
            return dir;

        if (dir.X != 0 && dir.Y != 0)
            dir = Math.Abs(dir.X) >= Math.Abs(dir.Y)
                ? new Vector2i(Math.Sign(dir.X), 0)
                : new Vector2i(0, Math.Sign(dir.Y));
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

    private bool CanOccupyTransform(EntityUid uid, EntityUid grid, Vector2 gridPos,
        Angle? overrideRotation = null, float clearance = 0f)
    {
        if (!physicsQ.TryComp(uid, out var body) || !fixtureQ.TryComp(uid, out var fixtures))
            return true;

        if (!body.CanCollide)
            return true;

        if (!gridQ.TryComp(grid, out var _))
            return true;

        var coords = new EntityCoordinates(grid, gridPos);
        var world = coords.ToMap(EntityManager, transform);

        var tileIndices = map.TileIndicesFor(grid, gridQ.GetComponent(grid), coords);
        DebugTestedTiles.Add((grid, tileIndices));

        var rotation = overrideRotation ?? transform.GetWorldRotation(uid);
        var tx = new Transform(world.Position, rotation);

        if (!TryGetFixtureAabb(fixtures, tx, out var aabb))
            return true;

        var myLayer = body.CollisionLayer;
        var myMask = body.CollisionMask;

        var hits = lookup.GetEntitiesIntersecting(world.MapId, aabb, LookupFlags.Dynamic | LookupFlags.Static);

        foreach (var other in hits)
        {
            if (other == uid)
                continue;

            if (!physicsQ.TryComp(other, out var otherBody) || !otherBody.CanCollide)
                continue;

            var otherLayer = otherBody.CollisionLayer;
            var otherMask = otherBody.CollisionMask;

            if ((myMask & otherLayer) == 0 && (myLayer & otherMask) == 0)
                continue;

            if (!fixtureQ.TryComp(other, out var otherFixtures))
                continue;

            var otherXform = Transform(other);
            var otherTx = physics.GetPhysicsTransform(other, otherXform);

            if (!TryGetFixtureAabb(otherFixtures, otherTx, out var otherAabb))
                continue;

            if (aabb.Intersects(otherAabb))
            {
                DebugCollisions.Add(new DebugCollision(uid, other, aabb, otherAabb, 0f, 0f, clearance));
                return false;
            }
        }

        return true;
    }

    private bool TryGetFixtureAabb(FixturesComponent fixtures, Transform transform, out Box2 aabb)
    {
        bool first = true;
        aabb = default;

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            for (int i = 0; i < fixture.Shape.ChildCount; i++)
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

    private Vector2i GetTile(EntityUid grid, MapGridComponent gridComp, Vector2 pos)
    {
        var coords = new EntityCoordinates(grid, pos);
        return map.TileIndicesFor(grid, gridComp, coords);
    }
}
