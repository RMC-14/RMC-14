using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Shared.Vehicle.Components;
using Content.Shared._RMC14.Vehicle;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.Shared.Vehicle;

public sealed partial class GridVehicleMoverSystem : EntitySystem
{
    private const float MinVehicleSpeed = 0.01f;
    private const float MinMoveDistance = 0.0001f;

    private void UpdateMovement(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        VehicleComponent vehicle,
        EntityUid grid,
        MapGridComponent gridComp,
        Vector2i inputDir,
        bool pushing,
        float frameTime)
    {
        if (vehicle.Operator != null)
        {
            var canRunEvent = new VehicleCanRunEvent((uid, vehicle));
            RaiseLocalEvent(uid, ref canRunEvent);
            if (!canRunEvent.CanRun)
            {
                StopMover(mover);
                SetGridPosition(uid, grid, mover.Position);
                Dirty(uid, mover);
                return;
            }
        }

        // Smash slowdowns expire independently of input, so check this before speed selection.
        GetSmashSlowdownMultiplier(mover);

        mover.IsCommittedToMove = false;
        if (!pushing)
        {
            mover.IsPushMove = false;
            mover.PushDirection = Vector2i.Zero;
        }

        var moved = pushing
            ? UpdatePushMovement(uid, mover, grid, gridComp, inputDir, frameTime)
            : UpdateDriveMovement(uid, mover, grid, gridComp, inputDir, frameTime);

        UpdateDerivedTileState(grid, gridComp, mover);
        mover.IsMoving = MathF.Abs(mover.CurrentSpeed) > MinVehicleSpeed;

        if (!mover.IsMoving)
        {
            mover.IsPushMove = false;
        }
        else if (!mover.IsPushMove)
        {
            PlayRunningSound(uid);
        }

        SetGridPosition(uid, grid, mover.Position);

        if (moved || mover.IsMoving)
            physics.WakeBody(uid);

        Dirty(uid, mover);
    }

    private bool UpdatePushMovement(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        MapGridComponent gridComp,
        Vector2i inputDir,
        float frameTime)
    {
        var hasInput = inputDir != Vector2i.Zero;
        if (hasInput)
        {
            mover.IsPushMove = true;
            mover.PushDirection = inputDir;
        }

        if (!hasInput && !mover.IsPushMove)
        {
            mover.CurrentSpeed = GridVehicleMotionSimulator.StepIdleSpeed(
                mover.CurrentSpeed,
                mover.Deceleration,
                frameTime);
            return false;
        }

        var maxSpeed = GetModifiedMaxSpeed(uid, mover);
        var accelModifier = GetAccelerationModifier(uid);
        if (hasInput && mover.PushImpulseSpeed > 0f)
        {
            var impulseSpeed = MathF.Min(mover.PushImpulseSpeed, maxSpeed);
            if (mover.CurrentSpeed < impulseSpeed)
                mover.CurrentSpeed = impulseSpeed;
        }

        mover.CurrentSpeed = GridVehicleMotionSimulator.StepPushSpeed(
            MathF.Max(0f, mover.CurrentSpeed),
            maxSpeed,
            mover.Acceleration * accelModifier,
            mover.Deceleration,
            hasInput,
            isCommittedToMove: false,
            frameTime);

        if (mover.PushDirection == Vector2i.Zero || mover.CurrentSpeed <= MinVehicleSpeed)
            return false;

        var moveDir = mover.PushDirection;
        var travel = mover.CurrentSpeed * frameTime;
        var moved = TryMoveWithLaneGuidance(
            uid,
            mover,
            grid,
            gridComp,
            moveDir,
            null,
            travel,
            frameTime,
            out var blocked);
        if (blocked)
            mover.CurrentSpeed = 0f;

        if (moved && hasInput && mover.PushCooldown > 0f)
            mover.NextPushTime = _timing.CurTime + TimeSpan.FromSeconds(mover.PushCooldown);

        return moved;
    }

    private bool UpdateDriveMovement(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        MapGridComponent gridComp,
        Vector2i inputDir,
        float frameTime)
    {
        var hasInput = inputDir != Vector2i.Zero;
        var facing = mover.CurrentDirection;
        var hadFacing = facing != Vector2i.Zero;

        if (hasInput && !hadFacing)
        {
            if (!TryApplyFacing(uid, mover, grid, gridComp, inputDir, startDelay: false, blockAfterTurn: false, allowMoveClearance: false))
            {
                mover.CurrentSpeed = 0f;
                return false;
            }

            facing = mover.CurrentDirection;
            hadFacing = true;
        }

        var reversing = hasInput && hadFacing && inputDir == -facing;
        var turnRequested = hasInput && hadFacing && !reversing && inputDir != facing;
        var turnInPlaceMaxSpeed = MathF.Max(0f, mover.TurnInPlaceMaxSpeed);
        var atTurnSpeed = MathF.Abs(mover.CurrentSpeed) <= turnInPlaceMaxSpeed;

        if (turnRequested)
        {
            if (!CanApplyTurn(mover))
            {
                if (MathF.Abs(mover.CurrentSpeed) <= MinVehicleSpeed)
                {
                    mover.CurrentSpeed = 0f;
                    return false;
                }
            }
            else if (mover.TurnInPlace && atTurnSpeed)
            {
                var turned = TryApplyFacing(uid, mover, grid, gridComp, inputDir, startDelay: true, blockAfterTurn: true, allowMoveClearance: false);
                mover.CurrentSpeed = 0f;
                return turned;
            }
            else if (TryApplyFacing(uid, mover, grid, gridComp, inputDir, startDelay: true, blockAfterTurn: false, allowMoveClearance: true))
            {
                facing = mover.CurrentDirection;
            }
            else if (MathF.Abs(mover.CurrentSpeed) <= MinVehicleSpeed)
            {
                mover.CurrentSpeed = 0f;
                return false;
            }
        }

        if (mover.TurnInPlace &&
            hasInput &&
            !reversing &&
            atTurnSpeed &&
            mover.InPlaceTurnBlockUntil > _timing.CurTime)
        {
            mover.CurrentSpeed = GridVehicleMotionSimulator.StepIdleSpeed(
                mover.CurrentSpeed,
                mover.Deceleration,
                frameTime);
            return false;
        }

        if (mover.CurrentDirection == Vector2i.Zero)
        {
            mover.CurrentSpeed = 0f;
            return false;
        }

        var profile = GetDriveProfile(uid, mover);
        var speedResult = hasInput
            ? GridVehicleMotionSimulator.StepDriveSpeed(
                mover.CurrentSpeed,
                profile,
                mover.CurrentDirection,
                inputDir,
                hasInput,
                isCommittedToMove: false,
                frameTime)
            : new GridVehicleMotionSimulator.DriveSpeedResult(
                GridVehicleMotionSimulator.StepIdleSpeed(
                    mover.CurrentSpeed,
                    mover.Deceleration,
                    frameTime),
                false,
                false);

        mover.CurrentSpeed = speedResult.CurrentSpeed;
        if (speedResult.ChangingDirection)
        {
            mover.CurrentSpeed = 0f;
            return false;
        }

        var travel = MathF.Abs(mover.CurrentSpeed) * frameTime;
        if (travel <= MinMoveDistance)
            return false;

        var moveDir = mover.CurrentSpeed >= 0f
            ? mover.CurrentDirection
            : -mover.CurrentDirection;
        var rotation = DirectionToVehicleRotation(mover.CurrentDirection);
        var moved = TryMoveWithLaneGuidance(
            uid,
            mover,
            grid,
            gridComp,
            moveDir,
            rotation,
            travel,
            frameTime,
            out var blocked);
        if (blocked)
            mover.CurrentSpeed = 0f;

        return moved;
    }

    private bool TryApplyFacing(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        MapGridComponent gridComp,
        Vector2i desiredFacing,
        bool startDelay,
        bool blockAfterTurn,
        bool allowMoveClearance)
    {
        if (desiredFacing == Vector2i.Zero)
            return false;

        var desiredRot = DirectionToVehicleRotation(desiredFacing);
        var immediateClear = TryFindTurnPosition(uid, mover, grid, gridComp, desiredRot, out var turnPosition);
        if (!immediateClear &&
            (!allowMoveClearance ||
             !TryFindTransientTurnClearance(uid, mover, grid, desiredFacing, desiredRot, out turnPosition)))
        {
            return false;
        }

        if (!CanOccupyTransform(uid, mover, grid, turnPosition, desiredRot, Clearance, applyEffects: true))
            return false;

        var turned = mover.CurrentDirection != desiredFacing;
        var moved = turnPosition != mover.Position;
        mover.Position = turnPosition;
        mover.CurrentTile = GetTile(grid, gridComp, mover.Position);
        mover.CurrentDirection = desiredFacing;
        transform.SetLocalRotation(uid, desiredRot);

        if (turned && startDelay)
        {
            StartTurnDelay(mover);
            if (blockAfterTurn && mover.TurnDelay > 0f)
                mover.InPlaceTurnBlockUntil = _timing.CurTime + TimeSpan.FromSeconds(mover.TurnDelay);
        }

        return turned || moved;
    }

    private bool TryFindTransientTurnClearance(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        Vector2i desiredFacing,
        Angle desiredRot,
        out Vector2 clearPosition)
    {
        clearPosition = mover.Position;

        var maxDistance = MathF.Max(0f, mover.TurnCollisionGraceDistance);
        if (maxDistance <= 0f)
            return false;

        var forward = new Vector2(desiredFacing.X, desiredFacing.Y);
        if (forward.LengthSquared() <= 0f)
            return false;

        var step = Math.Clamp(mover.MovementProbeStep, 0.02f, 0.5f);
        var steps = Math.Max(1, (int) MathF.Ceiling(maxDistance / step));
        var initialBlockers = new HashSet<EntityUid>();
        if (CanOccupyTransform(uid, mover, grid, mover.Position, desiredRot, Clearance, applyEffects: false, blockers: initialBlockers) ||
            initialBlockers.Count == 0)
        {
            return false;
        }

        var sampleBlockers = new HashSet<EntityUid>();
        for (var i = 1; i <= steps; i++)
        {
            var distance = MathF.Min(i * step, maxDistance);
            var sample = mover.Position + forward * distance;
            sampleBlockers.Clear();
            if (CanOccupyTransform(uid, mover, grid, sample, desiredRot, Clearance, applyEffects: false, blockers: sampleBlockers))
            {
                clearPosition = sample;
                return true;
            }

            foreach (var blocker in sampleBlockers)
            {
                if (!initialBlockers.Contains(blocker))
                    return false;
            }

            if (sampleBlockers.Count > 0)
                continue;

            return false;
        }

        return false;
    }

    private bool TryMoveWithLaneGuidance(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        MapGridComponent gridComp,
        Vector2i moveDir,
        Angle? rotation,
        float travel,
        float frameTime,
        out bool blocked)
    {
        blocked = false;
        var forward = new Vector2(moveDir.X, moveDir.Y);
        var directTarget = mover.Position + forward * travel;
        var moved = false;
        _directMoveBlockers.Clear();
        var ignoredEntities = GetPushIgnoredEntities(uid, mover);

        if (CanMoveContinuous(uid, mover, grid, directTarget, rotation, debugProbes: CollisionDebugEnabled, blockers: _directMoveBlockers, ignoredEntities: ignoredEntities))
        {
            AddDebugMovementDecision(uid, grid, mover.Position, directTarget, forward, DebugMovementDecisionKind.DirectClear, true);
            return TryMoveKnownClear(uid, mover, grid, directTarget, rotation, out blocked, ignoredEntities: ignoredEntities);
        }

        AddDebugMovementDecision(uid, grid, mover.Position, directTarget, forward, DebugMovementDecisionKind.DirectBlocked, false);

        if (TryGetBlockingMobBypassCorrection(
                uid,
                mover,
                grid,
                gridComp,
                moveDir,
                rotation,
                directTarget,
                frameTime,
                _directMoveBlockers,
                ignoredEntities,
                out var mobBypassCorrection))
        {
            moved = TryApplyLateralCorrection(uid, mover, grid, moveDir, rotation, mobBypassCorrection, ignoredEntities);
            if (moved)
                return true;
        }

        if (TryGetLaneCorrection(
                uid,
                mover,
                grid,
                gridComp,
                moveDir,
                rotation,
                directTarget,
                frameTime,
                ignoredEntities,
                out var correction))
        {
            moved = TryApplyLateralCorrection(uid, mover, grid, moveDir, rotation, correction, ignoredEntities);
        }

        var forwardStart = mover.Position;
        var forwardTarget = mover.Position + forward * travel;
        var forwardMoved = TryMoveContinuous(uid, mover, grid, forwardTarget, rotation, out blocked, ignoredEntities: ignoredEntities);

        AddDebugMovementDecision(
            uid,
            grid,
            forwardStart,
            forwardTarget,
            forward,
            blocked ? DebugMovementDecisionKind.ForwardBlocked : DebugMovementDecisionKind.ForwardAfterCorrection,
            forwardMoved && !blocked);
        return moved || forwardMoved;
    }

    private bool TryApplyLateralCorrection(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        Vector2i moveDir,
        Angle? rotation,
        float correction,
        HashSet<EntityUid>? ignoredEntities)
    {
        var lateralTarget = SetLateralCoordinate(
            mover.Position,
            moveDir,
            GetLateralCoordinate(mover.Position, moveDir) + correction);

        if ((lateralTarget - mover.Position).LengthSquared() <= MinMoveDistance * MinMoveDistance)
            return false;

        var lateralStart = mover.Position;
        var lateralDirection = lateralTarget - lateralStart;
        var moved = TryMoveContinuous(uid, mover, grid, lateralTarget, rotation, out _, applyBlockEffects: false, debugProbes: false, ignoredEntities: ignoredEntities);
        AddDebugMovementDecision(
            uid,
            grid,
            lateralStart,
            lateralTarget,
            lateralDirection,
            moved ? DebugMovementDecisionKind.LaneCorrection : DebugMovementDecisionKind.LaneCorrectionFailed,
            moved);

        return moved;
    }

    private static void AddDebugMovementDecision(
        EntityUid uid,
        EntityUid grid,
        Vector2 start,
        Vector2 end,
        Vector2 moveDirection,
        DebugMovementDecisionKind kind,
        bool success)
    {
        if (!MovementDebugEnabled)
            return;

        if (moveDirection.LengthSquared() > 0.0001f)
            moveDirection = Vector2.Normalize(moveDirection);
        else
            moveDirection = Vector2.Zero;

        DebugMovementDecisions.Add(new DebugMovementDecision(uid, grid, start, end, moveDirection, kind, success));
    }

    private bool CanMoveContinuous(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        Vector2 target,
        Angle? rotation,
        bool debugProbes)
        => CanMoveContinuous(uid, mover, grid, target, rotation, debugProbes, blockers: null, ignoredEntities: null);

    private bool CanMoveContinuous(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        Vector2 target,
        Angle? rotation,
        bool debugProbes,
        HashSet<EntityUid>? blockers,
        HashSet<EntityUid>? ignoredEntities = null)
    {
        var start = mover.Position;
        var delta = target - start;
        var distance = delta.Length();
        if (distance <= MinMoveDistance)
            return true;

        var probeStep = Math.Clamp(mover.MovementProbeStep, 0.02f, 0.5f);
        var steps = Math.Max(1, (int) MathF.Ceiling(distance / probeStep));

        for (var i = 1; i <= steps; i++)
        {
            var candidate = start + delta * (i / (float) steps);
            if (!CanOccupyTransform(uid, mover, grid, candidate, rotation, Clearance, applyEffects: false, debug: debugProbes, blockers: blockers, ignoredEntities: ignoredEntities))
                return false;
        }

        return true;
    }

    private bool TryGetBlockingMobBypassCorrection(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        MapGridComponent gridComp,
        Vector2i moveDir,
        Angle? rotation,
        Vector2 target,
        float frameTime,
        HashSet<EntityUid> directBlockers,
        HashSet<EntityUid>? ignoredEntities,
        out float correction)
    {
        correction = 0f;

        if (!HasBlockingVehicleMob(mover, directBlockers))
            return false;

        if (!TryFindBlockingMobBypassOffset(uid, mover, grid, gridComp, moveDir, rotation, target, ignoredEntities, out var laneOffset))
            return false;

        return TryGetLateralCorrection(mover, grid, gridComp, moveDir, target, laneOffset, frameTime, out correction);
    }

    private bool TryFindBlockingMobBypassOffset(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        MapGridComponent gridComp,
        Vector2i moveDir,
        Angle? rotation,
        Vector2 target,
        HashSet<EntityUid>? ignoredEntities,
        out float laneOffset)
    {
        laneOffset = 0f;
        var limit = Math.Clamp(MathF.Max(mover.TileOffsetLimit, mover.BlockingMobBypassNudgeLimit), 0f, 3f);
        if (limit <= 0f || moveDir == Vector2i.Zero)
            return false;

        var configuredStep = mover.BlockingMobBypassNudgeStep > 0f
            ? mover.BlockingMobBypassNudgeStep
            : mover.TileOffsetStep;
        var step = Math.Clamp(configuredStep, 0.01f, limit);
        var targetTile = GetTile(grid, gridComp, target);
        var center = GetTileCenter(targetTile);
        var centerLateral = GetLateralCoordinate(center, moveDir);
        var baseOffset = Math.Clamp(GetLateralCoordinate(target, moveDir) - centerLateral, -limit, limit);
        var lookahead = Math.Max(1, mover.TileOffsetLookahead);
        var sampleSteps = (int) MathF.Ceiling(limit / step);
        var lastPositiveOffset = float.NaN;
        var lastNegativeOffset = float.NaN;

        for (var i = 1; i <= sampleSteps; i++)
        {
            var distance = MathF.Min(i * step, limit);

            if (TryBlockingMobBypassOffset(uid, mover, grid, gridComp, moveDir, rotation, target, baseOffset + distance, limit, centerLateral, lookahead, ignoredEntities, ref lastPositiveOffset, out laneOffset))
                return true;

            if (TryBlockingMobBypassOffset(uid, mover, grid, gridComp, moveDir, rotation, target, baseOffset - distance, limit, centerLateral, lookahead, ignoredEntities, ref lastNegativeOffset, out laneOffset))
                return true;
        }

        return false;
    }

    private bool TryBlockingMobBypassOffset(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        MapGridComponent gridComp,
        Vector2i moveDir,
        Angle? rotation,
        Vector2 target,
        float offset,
        float limit,
        float centerLateral,
        int lookahead,
        HashSet<EntityUid>? ignoredEntities,
        ref float lastOffset,
        out float laneOffset)
    {
        laneOffset = Math.Clamp(offset, -limit, limit);
        if (MathF.Abs(laneOffset - lastOffset) <= 0.001f)
            return false;

        lastOffset = laneOffset;

        var desiredLateral = centerLateral + laneOffset;
        var lateralTarget = SetLateralCoordinate(mover.Position, moveDir, desiredLateral);
        if ((lateralTarget - mover.Position).LengthSquared() <= MinMoveDistance * MinMoveDistance)
            return false;

        if (!CanMoveContinuous(uid, mover, grid, lateralTarget, rotation, debugProbes: false, blockers: null, ignoredEntities: ignoredEntities))
            return false;

        return CanOccupyMoveLane(uid, mover, grid, gridComp, moveDir, rotation, target, laneOffset, lookahead, ignoredEntities);
    }

    private bool TryGetLaneCorrection(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        MapGridComponent gridComp,
        Vector2i moveDir,
        Angle? rotation,
        Vector2 target,
        float frameTime,
        HashSet<EntityUid>? ignoredEntities,
        out float correction)
    {
        correction = 0f;

        if (!TryFindBestLaneOffset(uid, mover, grid, gridComp, moveDir, rotation, target, ignoredEntities, out var laneOffset))
            return false;

        return TryGetLateralCorrection(mover, grid, gridComp, moveDir, target, laneOffset, frameTime, out correction);
    }

    private bool TryGetLateralCorrection(
        GridVehicleMoverComponent mover,
        EntityUid grid,
        MapGridComponent gridComp,
        Vector2i moveDir,
        Vector2 target,
        float laneOffset,
        float frameTime,
        out float correction)
    {
        var targetTile = GetTile(grid, gridComp, target);
        var center = GetTileCenter(targetTile);
        var currentLateral = GetLateralCoordinate(mover.Position, moveDir);
        var desiredLateral = GetLateralCoordinate(center, moveDir) + laneOffset;
        var correctionSpeed = MathF.Max(0f, mover.LaneCorrectionSpeed);
        if (correctionSpeed <= 0f)
        {
            correction = 0f;
            return false;
        }

        var maxCorrection = correctionSpeed * frameTime;
        correction = Math.Clamp(desiredLateral - currentLateral, -maxCorrection, maxCorrection);
        return MathF.Abs(correction) > MinMoveDistance;
    }

    private bool TryFindBestLaneOffset(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        MapGridComponent gridComp,
        Vector2i moveDir,
        Angle? rotation,
        Vector2 target,
        HashSet<EntityUid>? ignoredEntities,
        out float laneOffset)
    {
        laneOffset = 0f;
        var limit = Math.Clamp(mover.TileOffsetLimit, 0f, 2f);
        if (limit <= 0f || moveDir == Vector2i.Zero)
            return false;

        var step = Math.Clamp(mover.TileOffsetStep, 0.01f, limit);
        var targetTile = GetTile(grid, gridComp, target);
        var center = GetTileCenter(targetTile);
        var baseOffset = Math.Clamp(
            GetLateralCoordinate(target, moveDir) - GetLateralCoordinate(center, moveDir),
            -limit,
            limit);
        var sampleSteps = (int) MathF.Ceiling(limit / step);
        var lookahead = Math.Max(1, mover.TileOffsetLookahead);
        var foundLane = false;
        var bestOffset = baseOffset;
        var bestScore = float.MaxValue;
        var inLane = false;
        var laneStart = 0f;
        var laneEnd = 0f;

        for (var i = -sampleSteps; i <= sampleSteps; i++)
        {
            var offset = Math.Clamp(i * step, -limit, limit);
            var valid = CanOccupyMoveLane(uid, mover, grid, gridComp, moveDir, rotation, target, offset, lookahead, ignoredEntities);
            if (valid)
            {
                if (!inLane)
                {
                    inLane = true;
                    laneStart = offset;
                }

                laneEnd = offset;
                continue;
            }

            if (!inLane)
                continue;

            foundLane = true;
            SelectMoveLane(laneStart, laneEnd, baseOffset, step, ref bestOffset, ref bestScore);
            inLane = false;
        }

        if (inLane)
        {
            foundLane = true;
            SelectMoveLane(laneStart, laneEnd, baseOffset, step, ref bestOffset, ref bestScore);
        }

        if (!foundLane)
            return false;

        laneOffset = bestOffset;
        return true;
    }

    private bool CanOccupyMoveLane(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        MapGridComponent gridComp,
        Vector2i moveDir,
        Angle? rotation,
        Vector2 target,
        float offset,
        int lookahead,
        HashSet<EntityUid>? ignoredEntities)
    {
        var forward = new Vector2(moveDir.X, moveDir.Y);
        var tileSize = MathF.Max(1f, gridComp.TileSize);

        for (var i = 0; i < lookahead; i++)
        {
            var sample = target + forward * (tileSize * i);
            var tile = GetTile(grid, gridComp, sample);
            var center = GetTileCenter(tile);
            var lateral = GetLateralCoordinate(center, moveDir) + offset;
            sample = SetLateralCoordinate(sample, moveDir, lateral);

            if (!CanOccupyTransform(uid, mover, grid, sample, rotation, Clearance, applyEffects: false, debug: false, ignoredEntities: ignoredEntities))
                return false;
        }

        return true;
    }

    private bool TryMoveContinuous(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        Vector2 target,
        Angle? rotation,
        out bool blocked,
        bool applyBlockEffects = true,
        bool debugProbes = true,
        HashSet<EntityUid>? ignoredEntities = null)
    {
        blocked = false;
        var start = mover.Position;
        var delta = target - start;
        var distance = delta.Length();
        if (distance <= MinMoveDistance)
            return false;

        var probeStep = Math.Clamp(mover.MovementProbeStep, 0.02f, 0.5f);
        var steps = Math.Max(1, (int) MathF.Ceiling(distance / probeStep));
        var lastGood = start;

        for (var i = 1; i <= steps; i++)
        {
            var candidate = start + delta * (i / (float) steps);
            if (!CanOccupyTransform(uid, mover, grid, candidate, rotation, Clearance, applyEffects: false, debug: debugProbes, ignoredEntities: ignoredEntities))
            {
                if (applyBlockEffects)
                    CanOccupyTransform(uid, mover, grid, candidate, rotation, Clearance, applyEffects: true, debug: false, ignoredEntities: ignoredEntities);

                mover.Position = lastGood;
                blocked = true;
                return lastGood != start;
            }

            lastGood = candidate;
        }

        if (applyBlockEffects &&
            !CanOccupyTransform(uid, mover, grid, lastGood, rotation, Clearance, applyEffects: true, debug: false, ignoredEntities: ignoredEntities))
        {
            mover.Position = start;
            blocked = true;
            return false;
        }

        mover.Position = lastGood;
        return true;
    }

    private bool TryMoveKnownClear(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        Vector2 target,
        Angle? rotation,
        out bool blocked,
        bool applyBlockEffects = true,
        HashSet<EntityUid>? ignoredEntities = null)
    {
        blocked = false;

        if (applyBlockEffects &&
            !CanOccupyTransform(uid, mover, grid, target, rotation, Clearance, applyEffects: true, debug: false, ignoredEntities: ignoredEntities))
        {
            blocked = true;
            return false;
        }

        mover.Position = target;
        return true;
    }

    private bool TryFindTurnPosition(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        MapGridComponent gridComp,
        Angle desiredRot,
        out Vector2 turnPosition)
    {
        turnPosition = mover.Position;
        if (CanOccupyTransform(uid, mover, grid, turnPosition, desiredRot, Clearance, applyEffects: false))
            return true;

        var limit = Math.Clamp(mover.TurnNudgeLimit, 0f, 0.49f);
        if (limit <= 0f)
            return false;

        var step = Math.Clamp(mover.TurnNudgeStep, 0.01f, limit);
        var currentTile = GetTile(grid, gridComp, mover.Position);
        var center = GetTileCenter(currentTile);
        var min = center - new Vector2(limit, limit);
        var max = center + new Vector2(limit, limit);

        var steps = (int) MathF.Ceiling(limit / step);
        for (var ring = 1; ring <= steps; ring++)
        {
            var axialDistance = Math.Clamp(ring * step, -limit, limit);
            if (TryTurnNudgePosition(uid, mover, grid, gridComp, currentTile, desiredRot, new Vector2(axialDistance, 0f), min, max, out turnPosition))
                return true;

            if (TryTurnNudgePosition(uid, mover, grid, gridComp, currentTile, desiredRot, new Vector2(-axialDistance, 0f), min, max, out turnPosition))
                return true;

            if (TryTurnNudgePosition(uid, mover, grid, gridComp, currentTile, desiredRot, new Vector2(0f, axialDistance), min, max, out turnPosition))
                return true;

            if (TryTurnNudgePosition(uid, mover, grid, gridComp, currentTile, desiredRot, new Vector2(0f, -axialDistance), min, max, out turnPosition))
                return true;

            for (var x = -ring; x <= ring; x++)
            {
                for (var y = -ring; y <= ring; y++)
                {
                    if (Math.Max(Math.Abs(x), Math.Abs(y)) != ring)
                        continue;

                    if (x == 0 || y == 0)
                        continue;

                    var offset = new Vector2(
                        Math.Clamp(x * step, -limit, limit),
                        Math.Clamp(y * step, -limit, limit));

                    if (TryTurnNudgePosition(uid, mover, grid, gridComp, currentTile, desiredRot, offset, min, max, out turnPosition))
                        return true;
                }
            }
        }

        return false;
    }

    private bool TryTurnNudgePosition(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        MapGridComponent gridComp,
        Vector2i currentTile,
        Angle desiredRot,
        Vector2 offset,
        Vector2 min,
        Vector2 max,
        out Vector2 turnPosition)
    {
        turnPosition = new Vector2(
            Math.Clamp(mover.Position.X + offset.X, min.X, max.X),
            Math.Clamp(mover.Position.Y + offset.Y, min.Y, max.Y));

        if (turnPosition == mover.Position)
            return false;

        if (GetTile(grid, gridComp, turnPosition) != currentTile)
            return false;

        return CanOccupyTransform(uid, mover, grid, turnPosition, desiredRot, Clearance, applyEffects: false);
    }

    private void SelectMoveLane(
        float laneStart,
        float laneEnd,
        float baseOffset,
        float step,
        ref float bestOffset,
        ref float bestScore)
    {
        var laneWidth = laneEnd - laneStart;
        var center = (laneStart + laneEnd) * 0.5f;
        var margin = MathF.Min(step * 2f, laneWidth * 0.5f);
        var safeStart = laneStart + margin;
        var safeEnd = laneEnd - margin;
        var offset = safeStart <= safeEnd
            ? Math.Clamp(baseOffset, safeStart, safeEnd)
            : center;
        var distance = MathF.Abs(offset - baseOffset);
        var score = distance - laneWidth * 0.25f;

        if (score >= bestScore)
            return;

        bestScore = score;
        bestOffset = offset;
    }

    private GridVehicleMotionSimulator.DriveProfile GetDriveProfile(EntityUid uid, GridVehicleMoverComponent mover)
    {
        var accelModifier = GetAccelerationModifier(uid);
        return new GridVehicleMotionSimulator.DriveProfile(
            GetModifiedMaxSpeed(uid, mover),
            GetModifiedMaxReverseSpeed(uid, mover),
            mover.Acceleration * accelModifier,
            mover.ReverseAcceleration * accelModifier,
            mover.Deceleration);
    }

    private float GetModifiedMaxSpeed(EntityUid uid, GridVehicleMoverComponent mover)
    {
        var maxSpeed = mover.MaxSpeed * GetSmashSlowdownMultiplier(mover);

        if (TryComp<VehicleOverchargeComponent>(uid, out var overcharge) && _timing.CurTime < overcharge.ActiveUntil)
            maxSpeed *= overcharge.SpeedMultiplier;
        if (TryComp<VehicleSpeedModifierComponent>(uid, out var speedMod))
            maxSpeed *= speedMod.SpeedMultiplier;

        return maxSpeed;
    }

    private float GetModifiedMaxReverseSpeed(EntityUid uid, GridVehicleMoverComponent mover)
    {
        var maxSpeed = mover.MaxReverseSpeed * GetSmashSlowdownMultiplier(mover);

        if (TryComp<VehicleOverchargeComponent>(uid, out var overcharge) && _timing.CurTime < overcharge.ActiveUntil)
            maxSpeed *= overcharge.SpeedMultiplier;
        if (TryComp<VehicleSpeedModifierComponent>(uid, out var speedMod))
            maxSpeed *= speedMod.SpeedMultiplier;

        return maxSpeed;
    }

    private float GetAccelerationModifier(EntityUid uid)
    {
        if (TryComp<VehicleAccelerationModifierComponent>(uid, out var accelMod))
            return MathF.Max(0.05f, accelMod.AccelerationMultiplier);

        return 1f;
    }

    private void StopMover(GridVehicleMoverComponent mover)
    {
        mover.CurrentSpeed = 0f;
        mover.IsCommittedToMove = false;
        mover.IsPushMove = false;
        mover.IsMoving = false;
        mover.TargetPosition = mover.Position;
        mover.TargetTile = mover.CurrentTile;
        mover.PushDirection = Vector2i.Zero;
    }

    private void UpdateDerivedTileState(EntityUid grid, MapGridComponent gridComp, GridVehicleMoverComponent mover)
    {
        var tile = GetTile(grid, gridComp, mover.Position);
        mover.CurrentTile = tile;
        mover.TargetTile = tile;
        mover.TargetPosition = mover.Position;
    }

    private static Angle DirectionToVehicleRotation(Vector2i direction)
    {
        return new Vector2(direction.X, direction.Y).ToWorldAngle();
    }

    private static float GetLateralCoordinate(Vector2 position, Vector2i moveDir)
    {
        return moveDir.X != 0 ? position.Y : position.X;
    }

    private static Vector2 SetLateralCoordinate(Vector2 position, Vector2i moveDir, float lateral)
    {
        if (moveDir.X != 0)
            position.Y = lateral;
        else
            position.X = lateral;

        return position;
    }

    private static Vector2 GetTileCenter(Vector2i tile)
    {
        return new Vector2(tile.X + 0.5f, tile.Y + 0.5f);
    }

    private HashSet<EntityUid>? GetPushIgnoredEntities(EntityUid uid, GridVehicleMoverComponent mover)
    {
        if (!mover.IsPushMove)
            return null;

        if (!_activeXenoPushers.TryGetValue(uid, out var pusher))
            return null;

        if (!pusher.IsValid() || TerminatingOrDeleted(pusher))
        {
            _activeXenoPushers.Remove(uid);
            return null;
        }

        _pushIgnoredEntities.Clear();
        _pushIgnoredEntities.Add(pusher);
        return _pushIgnoredEntities;
    }

    private bool CanApplyTurn(GridVehicleMoverComponent mover)
    {
        if (mover.TurnDelay <= 0f)
            return true;

        return _timing.CurTime >= mover.NextTurnTime;
    }

    private void StartTurnDelay(GridVehicleMoverComponent mover)
    {
        if (mover.TurnDelay <= 0f)
            return;

        mover.NextTurnTime = _timing.CurTime + TimeSpan.FromSeconds(mover.TurnDelay);
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

    private Vector2i GetTile(EntityUid grid, MapGridComponent gridComp, Vector2 pos)
    {
        var coords = new EntityCoordinates(grid, pos);
        return map.TileIndicesFor(grid, gridComp, coords);
    }

    private void PlayRunningSound(EntityUid uid)
    {
        if (!TryComp<VehicleSoundComponent>(uid, out var sound))
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

    private void ApplySmashSlowdown(EntityUid vehicle, GridVehicleMoverComponent mover, VehicleSmashableComponent smashable)
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
}
