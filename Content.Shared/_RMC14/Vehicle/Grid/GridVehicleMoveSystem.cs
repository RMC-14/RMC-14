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
using Robust.Shared.Utility;

namespace Content.Shared.Vehicle;

public sealed class GridVehicleMoverSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming timing = default!;
    [Dependency] private readonly SharedTransformSystem transform = default!;
    [Dependency] private readonly SharedMapSystem map = default!;
    [Dependency] private readonly SharedPhysicsSystem physics = default!;

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

            // Get input direction
            Vector2i inputDir = Vector2i.Zero;
            if (vehicle.Operator is { } op && TryComp<InputMoverComponent>(op, out var input))
            {
                inputDir = GetInputDirection(input);
            }

            // Update movement
            UpdateMovement(uid, mover, grid, gridComp, inputDir, frameTime);
        }
    }

    private void UpdateMovement(EntityUid uid, GridVehicleMoverComponent mover, EntityUid grid,
        MapGridComponent gridComp, Vector2i inputDir, float frameTime)
    {
        var hasInput = inputDir != Vector2i.Zero;

        // If we're committed to a move, we must complete it
        if (mover.IsCommittedToMove)
        {
            ContinueCommittedMove(uid, mover, grid, gridComp, inputDir, frameTime);
            return;
        }

        // Not committed - check if we should start a new move
        if (!hasInput)
        {
            // No input and not committed - ensure we're centered on current tile
            mover.CurrentSpeed = 0f;
            mover.Position = new Vector2(mover.CurrentTile.X + 0.5f, mover.CurrentTile.Y + 0.5f);
            SetGridPosition(uid, grid, mover.Position);
            Dirty(uid, mover);
            return;
        }

        // New input - start a committed move
        var targetTile = mover.CurrentTile + inputDir;

        // Check if we can move to the target tile
        if (!CanBeOnTile(uid, grid, gridComp, targetTile))
        {
            // Can't move there, stay stopped
            mover.CurrentSpeed = 0f;
            return;
        }

        // Commit to moving to the target tile
        mover.IsCommittedToMove = true;
        mover.TargetTile = targetTile;
        mover.CurrentDirection = inputDir;

        // Update rotation immediately
        var angle = new Vector2(inputDir.X, inputDir.Y).ToWorldAngle();
        transform.SetLocalRotation(uid, angle);

        Dirty(uid, mover);
    }

    private void ContinueCommittedMove(EntityUid uid, GridVehicleMoverComponent mover, EntityUid grid,
        MapGridComponent gridComp, Vector2i inputDir, float frameTime)
    {
        // Accelerate towards max speed
        if (mover.CurrentSpeed < mover.MaxSpeed)
        {
            mover.CurrentSpeed = Math.Min(mover.CurrentSpeed + mover.Acceleration * frameTime, mover.MaxSpeed);
        }

        // Move in the committed direction
        var moveDir = new Vector2(mover.CurrentDirection.X, mover.CurrentDirection.Y);
        var moveAmount = mover.CurrentSpeed * frameTime;
        var newPos = mover.Position + moveDir * moveAmount;

        // Calculate center of target tile
        var targetCenter = new Vector2(mover.TargetTile.X + 0.5f, mover.TargetTile.Y + 0.5f);

        // Check if we've reached or passed the target
        var toTarget = targetCenter - mover.Position;
        var distanceToTarget = toTarget.Length();

        if (moveAmount >= distanceToTarget)
        {
            // We've reached the target tile
            mover.Position = targetCenter;
            mover.CurrentTile = mover.TargetTile;
            mover.IsCommittedToMove = false;

            // If player is still holding input in a direction, queue next move
            var hasInput = inputDir != Vector2i.Zero;
            if (hasInput)
            {
                // Check if direction changed
                if (inputDir != mover.CurrentDirection)
                {
                    // Direction changed - update rotation
                    mover.CurrentDirection = inputDir;
                    var angle = new Vector2(inputDir.X, inputDir.Y).ToWorldAngle();
                    transform.SetLocalRotation(uid, angle);
                }

                // Try to start next move immediately
                var nextTile = mover.CurrentTile + inputDir;
                if (CanBeOnTile(uid, grid, gridComp, nextTile))
                {
                    mover.IsCommittedToMove = true;
                    mover.TargetTile = nextTile;
                }
                else
                {
                    // Hit a wall, stop
                    mover.CurrentSpeed = 0f;
                }
            }
            else
            {
                // No input, decelerate
                mover.CurrentSpeed = Math.Max(mover.CurrentSpeed - mover.Deceleration * frameTime, 0f);
            }
        }
        else
        {
            // Still moving towards target
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

        // Prioritize single direction (no diagonals for grid movement)
        if (dir.X != 0 && dir.Y != 0)
        {
            if (Math.Abs(dir.X) >= Math.Abs(dir.Y))
                dir = new Vector2i(Math.Sign(dir.X), 0);
            else
                dir = new Vector2i(0, Math.Sign(dir.Y));
        }
        else
        {
            dir = new Vector2i(Math.Sign(dir.X), Math.Sign(dir.Y));
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

    private bool CanBeOnTile(EntityUid uid, EntityUid grid, MapGridComponent gridComp, Vector2i tile)
    {
        if (!physicsQ.TryComp(uid, out var phys) || !phys.CanCollide)
            return true;

        var coords = new EntityCoordinates(grid, new Vector2(tile.X + 0.5f, tile.Y + 0.5f));
        DebugTools.Assert(grid == coords.EntityId);

        var indices = map.TileIndicesFor(grid, gridComp, coords);
        var enumerator = map.GetAnchoredEntitiesEnumerator(grid, gridComp, indices);

        var (moverLayer, moverMask) = physics.GetHardCollision(uid);

        while (enumerator.MoveNext(out var anchored))
        {
            if (!physicsQ.TryComp(anchored, out var anchoredPhys) || !anchoredPhys.CanCollide)
                continue;

            if (!fixtureQ.TryComp(anchored, out var fixture))
                continue;

            var (anchoredLayer, anchoredMask) = physics.GetHardCollision(anchored.Value, fixture);

            if ((anchoredLayer & moverMask) != 0)
                return false;

            if ((anchoredMask & moverLayer) != 0)
                return false;
        }

        return true;
    }
}
