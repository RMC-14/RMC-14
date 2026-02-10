using System;
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
    private const int MaxTileStepsPerFrame = 4;

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
                mover.CurrentSpeed = 0f;
                mover.IsCommittedToMove = false;
                mover.IsMoving = false;
                SetGridPosition(uid, grid, mover.Position);
                Dirty(uid, mover);
                return;
            }
        }

        // Ensure smash slowdowns expire even if the vehicle is idle.
        GetSmashSlowdownMultiplier(mover);

        var isPushMove = mover.IsPushMove || pushing;
        if (mover.IsCommittedToMove)
        {
            if (mover.IsPushMove)
                ContinueCommittedPush(uid, mover, grid, gridComp, inputDir, frameTime);
            else
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
            if (!mover.IsMoving)
                mover.IsPushMove = false;
            if (mover.IsMoving && !isPushMove)
                PlayRunningSound(uid);
            SetGridPosition(uid, grid, mover.Position);
            Dirty(uid, mover);
            return;
        }

        if (pushing)
            CommitPushTile(uid, mover, grid, gridComp, inputDir);
        else
            CommitNextTile(uid, mover, grid, gridComp, inputDir);
        SetGridPosition(uid, grid, mover.Position);
        Dirty(uid, mover);
    }

    private void ContinueCommittedPush(
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
            mover.IsPushMove = false;
            return;
        }

        var maxSpeed = mover.MaxSpeed;
        var smashMultiplier = GetSmashSlowdownMultiplier(mover);
        maxSpeed *= smashMultiplier;

        if (TryComp<RMCVehicleOverchargeComponent>(uid, out var overcharge) && _timing.CurTime < overcharge.ActiveUntil)
            maxSpeed *= overcharge.SpeedMultiplier;

        var targetCenter = new Vector2(mover.TargetTile.X + 0.5f, mover.TargetTile.Y + 0.5f);
        var toTarget = targetCenter - mover.Position;
        var distToTarget = toTarget.Length();

        var hasInput = inputDir != Vector2i.Zero;
        var hasInputForSpeed = hasInput || mover.IsCommittedToMove;
        float targetSpeed;
        float accel;

        if (!hasInputForSpeed)
        {
            targetSpeed = 0f;
            accel = mover.Deceleration;
        }
        else
        {
            targetSpeed = maxSpeed;
            accel = mover.Acceleration;
        }

        if (mover.CurrentSpeed < targetSpeed)
            mover.CurrentSpeed = MathF.Min(mover.CurrentSpeed + accel * frameTime, targetSpeed);
        else if (mover.CurrentSpeed > targetSpeed)
            mover.CurrentSpeed = MathF.Max(mover.CurrentSpeed - mover.Deceleration * frameTime, targetSpeed);

        var speedMag = MathF.Abs(mover.CurrentSpeed);
        var remaining = speedMag * frameTime;
        var steps = 0;

        while (true)
        {
            if (distToTarget <= 0.0001f || remaining >= distToTarget)
            {
                mover.Position = targetCenter;
                mover.CurrentTile = mover.TargetTile;
                remaining -= distToTarget;

                if (!hasInput || MathF.Abs(mover.CurrentSpeed) <= 0.0001f)
                {
                    mover.IsCommittedToMove = false;
                    mover.IsPushMove = false;
                    mover.CurrentSpeed = 0f;
                    break;
                }

                CommitPushTile(uid, mover, grid, gridComp, inputDir);
                if (!mover.IsCommittedToMove)
                    break;

                if (++steps >= MaxTileStepsPerFrame || remaining <= 0f)
                    break;

                targetCenter = new Vector2(mover.TargetTile.X + 0.5f, mover.TargetTile.Y + 0.5f);
                toTarget = targetCenter - mover.Position;
                distToTarget = toTarget.Length();
                continue;
            }

            var dir = toTarget / distToTarget;
            mover.Position += dir * remaining;
            break;
        }

        mover.IsMoving = MathF.Abs(mover.CurrentSpeed) > 0.01f;
        SetGridPosition(uid, grid, mover.Position);
        physics.WakeBody(uid);
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
        var remaining = speedMag * frameTime;
        var steps = 0;

        while (true)
        {
            if (distToTarget <= 0.0001f || remaining >= distToTarget)
            {
                mover.Position = targetCenter;
                mover.CurrentTile = mover.TargetTile;
                remaining -= distToTarget;

                if (!hasInput || MathF.Abs(mover.CurrentSpeed) <= 0.0001f)
                {
                    mover.IsCommittedToMove = false;
                    mover.CurrentSpeed = 0f;
                    break;
                }

                CommitNextTile(uid, mover, grid, gridComp, inputDir);
                if (!mover.IsCommittedToMove)
                    break;

                if (++steps >= MaxTileStepsPerFrame || remaining <= 0f)
                    break;

                targetCenter = new Vector2(mover.TargetTile.X + 0.5f, mover.TargetTile.Y + 0.5f);
                toTarget = targetCenter - mover.Position;
                distToTarget = toTarget.Length();
                continue;
            }

            var dir = toTarget / distToTarget;
            mover.Position += dir * remaining;
            break;
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
            mover.IsPushMove = false;
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
            mover.IsPushMove = false;
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
        mover.IsPushMove = false;
    }

    private void CommitPushTile(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        MapGridComponent gridComp,
        Vector2i inputDir)
    {
        if (inputDir == Vector2i.Zero)
        {
            mover.IsCommittedToMove = false;
            mover.IsPushMove = false;
            return;
        }

        var targetTile = mover.CurrentTile + inputDir;
        var targetCenter = new Vector2(targetTile.X + 0.5f, targetTile.Y + 0.5f);

        if (!CanOccupyTransform(uid, mover, grid, targetCenter, null, Clearance))
        {
            mover.TargetTile = mover.CurrentTile;
            mover.IsCommittedToMove = false;
            mover.IsPushMove = false;
            mover.CurrentSpeed = 0f;
            return;
        }

        if (mover.PushCooldown > 0f)
            mover.NextPushTime = _timing.CurTime + TimeSpan.FromSeconds(mover.PushCooldown);

        mover.TargetTile = targetTile;
        mover.IsCommittedToMove = true;
        mover.IsPushMove = true;
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
}
