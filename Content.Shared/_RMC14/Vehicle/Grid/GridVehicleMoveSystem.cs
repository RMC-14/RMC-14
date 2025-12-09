using System;
using System.Numerics;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Vehicle.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
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
        Logger.Info($"Vehicle DEBUG Startup tile={indices}");
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
        if (inputDir != Vector2i.Zero || mover.IsCommittedToMove)
            Logger.Info($"Vehicle DEBUG Update uid={uid} tile={mover.CurrentTile} speed={mover.CurrentSpeed} input={inputDir} committed={mover.IsCommittedToMove}");

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
        Logger.Info($"Vehicle DEBUG requestMove moveDir={moveDir} facing={facing} targetTile={targetTile}");

        if (!CanBeOnTile(uid, grid, gridComp, targetTile))
        {
            mover.CurrentSpeed = 0f;
            Logger.Info($"Vehicle DEBUG blockedTile");
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
        Logger.Info($"Vehicle DEBUG commitTile target={targetTile}");
    }

    private void ContinueCommittedMove(EntityUid uid, GridVehicleMoverComponent mover, EntityUid grid,
        MapGridComponent gridComp, Vector2i inputDir, float frameTime)
    {
        var tileDelta = mover.TargetTile - mover.CurrentTile;
        var moveDir = new Vector2(tileDelta.X, tileDelta.Y);

        var hasInput = inputDir != Vector2i.Zero;
        var opposite = hasInput && inputDir == -mover.CurrentDirection;

        if (opposite)
            mover.CurrentSpeed = 0f;

        var tileDelta2 = mover.TargetTile - mover.CurrentTile;
        var reversing = hasInput && inputDir == -mover.CurrentDirection;

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

        Logger.Info($"Vehicle DEBUG movePhase speed={mover.CurrentSpeed} reversing={reversing} targetSpeed={targetSpeed}");

        if (moveDir == Vector2.Zero)
        {
            moveDir = new Vector2(mover.CurrentDirection.X, mover.CurrentDirection.Y);
        }

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
            Logger.Info($"Vehicle DEBUG tileArrived tile={mover.CurrentTile} speed={mover.CurrentSpeed}");

            if (reversing && mover.CurrentSpeed > 0.01f)
            {
                Logger.Info($"Vehicle DEBUG stopCommit slowingForward");
                return;
            }

            if (!reversing && mover.CurrentSpeed < -0.01f)
            {
                Logger.Info($"Vehicle DEBUG stopCommit slowingReverse");
                return;
            }

            var hasInput2 = inputDir != Vector2i.Zero;

            if (!hasInput2)
            {
                mover.CurrentSpeed = Math.Max(mover.CurrentSpeed - mover.Deceleration * frameTime, 0f);
                Logger.Info($"Vehicle DEBUG noInputDecel");
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
                Logger.Info($"Vehicle DEBUG nextTileBlocked");
                return;
            }

            if (Math.Abs(mover.CurrentSpeed) < 0.01f)
            {
                Logger.Info($"Vehicle DEBUG notMovingEnoughToCommit");
                return;
            }

            mover.IsCommittedToMove = true;
            mover.TargetTile = nextTile;
            mover.CurrentDirection = facing;

            if (angleChanged)
            {
                var angle = new Vector2(facing.X, facing.Y).ToWorldAngle();
                transform.SetLocalRotation(uid, angle);
            }

            Logger.Info($"Vehicle DEBUG nextCommit target={nextTile}");
        }
        else
        {
            mover.Position = newPos;
            Logger.Info($"Vehicle DEBUG moveStep pos={mover.Position}");
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

        Logger.Info($"Vehicle DEBUG input dir={dir}");
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
