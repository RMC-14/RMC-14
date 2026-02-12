using System;
using System.Numerics;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.Vehicle.Components;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.Graphics;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client.Vehicle;

public sealed class VehicleHardpointDebugOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private const float PixelsPerMeter = 32f;

    public bool Enabled { get; set; }

    private readonly IEntityManager _ents;
    private readonly SharedTransformSystem _transform;
    private readonly SharedContainerSystem _container;
    private readonly EntityQuery<GunComponent> _gunQ;
    private readonly EntityQuery<GunFireArcComponent> _fireArcQ;
    private readonly EntityQuery<GridVehicleMoverComponent> _moverQ;
    private readonly EntityQuery<GunMuzzleOffsetComponent> _muzzleQ;
    private readonly EntityQuery<VehicleTurretComponent> _turretQ;
    private readonly EntityQuery<VehicleTurretMuzzleComponent> _turretMuzzleQ;
    private readonly EntityQuery<VehiclePortGunComponent> _portGunQ;

    public VehicleHardpointDebugOverlay(IEntityManager ents)
    {
        _ents = ents;
        _transform = ents.System<SharedTransformSystem>();
        _container = ents.System<SharedContainerSystem>();
        _gunQ = ents.GetEntityQuery<GunComponent>();
        _fireArcQ = ents.GetEntityQuery<GunFireArcComponent>();
        _moverQ = ents.GetEntityQuery<GridVehicleMoverComponent>();
        _muzzleQ = ents.GetEntityQuery<GunMuzzleOffsetComponent>();
        _turretQ = ents.GetEntityQuery<VehicleTurretComponent>();
        _turretMuzzleQ = ents.GetEntityQuery<VehicleTurretMuzzleComponent>();
        _portGunQ = ents.GetEntityQuery<VehiclePortGunComponent>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!Enabled)
            return;

        var handle = args.WorldHandle;
        var query = _ents.EntityQueryEnumerator<GunMuzzleOffsetComponent>();

        while (query.MoveNext(out var uid, out var muzzle))
        {
            if (!_turretQ.HasComp(uid) && !_portGunQ.HasComp(uid))
                continue;

            if (!TryGetMuzzlePositions(uid, muzzle, args.MapId, out var origin, out var muzzlePos))
                continue;

            handle.DrawLine(origin, muzzlePos, new Color(0.95f, 0.95f, 0.95f, 0.7f));
            handle.DrawCircle(origin, 0.07f, new Color(0.2f, 0.9f, 1f, 0.9f));
            handle.DrawCircle(muzzlePos, 0.1f, new Color(1f, 0.75f, 0.2f, 0.95f));

            DrawShootArc(uid, origin, args.MapId, handle);
            DrawShootTarget(uid, origin, args.MapId, handle);
        }

        var turretMuzzleQuery = _ents.EntityQueryEnumerator<VehicleTurretMuzzleComponent>();
        while (turretMuzzleQuery.MoveNext(out var uid, out var turretMuzzle))
        {
            if (!_turretQ.HasComp(uid))
                continue;

            if (!TryGetTurretMuzzlePositions(uid, turretMuzzle, args.MapId, out var turretBasePos, out var basePos, out var leftPos, out var rightPos, out var leftRadius, out var rightRadius, out var useRightNext))
                continue;

            if (leftRadius > 0f)
                handle.DrawCircle(turretBasePos, leftRadius, new Color(0.25f, 0.85f, 1f, 0.5f));
            if (rightRadius > 0f && MathF.Abs(rightRadius - leftRadius) > 0.01f)
                handle.DrawCircle(turretBasePos, rightRadius, new Color(1f, 0.6f, 0.2f, 0.5f));

            var leftColor = useRightNext ? new Color(0.4f, 0.4f, 0.4f, 0.7f) : new Color(0.2f, 0.95f, 0.4f, 0.95f);
            var rightColor = useRightNext ? new Color(0.2f, 0.95f, 0.4f, 0.95f) : new Color(0.4f, 0.4f, 0.4f, 0.7f);
            if (leftRadius > 0f)
                handle.DrawCircle(leftPos, 0.08f, leftColor);
            if (rightRadius > 0f)
                handle.DrawCircle(rightPos, 0.08f, rightColor);
            handle.DrawLine(turretBasePos, basePos, new Color(0.4f, 0.9f, 1f, 0.5f));
        }

        var turretQuery = _ents.EntityQueryEnumerator<VehicleTurretComponent>();
        while (turretQuery.MoveNext(out var uid, out var turret))
        {
            if (!TryGetTurretOverlayPositions(uid, turret, args.MapId, out var basePos, out var anchorPos, out var turretPos))
                continue;

            handle.DrawCircle(basePos, 0.06f, new Color(0.7f, 0.7f, 0.7f, 0.8f));
            handle.DrawLine(basePos, anchorPos, new Color(0.2f, 0.8f, 0.95f, 0.7f));
            handle.DrawCircle(anchorPos, 0.07f, new Color(0.2f, 0.8f, 0.95f, 0.9f));

            if (anchorPos != turretPos)
            {
                handle.DrawLine(anchorPos, turretPos, new Color(1f, 0.7f, 0.2f, 0.8f));
                handle.DrawCircle(turretPos, 0.08f, new Color(1f, 0.7f, 0.2f, 0.95f));
            }
        }
    }

    private bool TryGetMuzzlePositions(
        EntityUid uid,
        GunMuzzleOffsetComponent muzzle,
        MapId mapId,
        out Vector2 origin,
        out Vector2 muzzlePos)
    {
        origin = default;
        muzzlePos = default;

        var baseUid = uid;
        if (muzzle.UseContainerOwner &&
            _container.TryGetContainingContainer((uid, null), out var container))
        {
            baseUid = container.Owner;
        }

        if (!_ents.TryGetComponent(baseUid, out TransformComponent? baseXform))
            return false;
        if (baseXform.MapID != mapId)
            return false;

        var baseCoords = _transform.GetMoverCoordinates(baseUid);
        var baseRotation = GetBaseRotation(baseUid, muzzle.AngleOffset);

        var (offset, rotateOffset) = GetOffset(muzzle, baseUid, baseRotation);
        var originCoords = rotateOffset
            ? baseCoords.Offset(baseRotation.RotateVec(offset))
            : baseCoords.Offset(offset);
        origin = _transform.ToMapCoordinates(originCoords).Position;

        var muzzleCoords = originCoords;
        var muzzleRotation = baseRotation;

        if (muzzle.MuzzleOffset != Vector2.Zero)
        {
            if (muzzle.UseAimDirection &&
                _gunQ.TryComp(uid, out var gun) &&
                gun.ShootCoordinates is { } shootCoords)
            {
                var pivotMap = _transform.ToMapCoordinates(originCoords);
                var targetMap = _transform.ToMapCoordinates(shootCoords);
                var direction = targetMap.Position - pivotMap.Position;
                if (direction.LengthSquared() > 0.0001f)
                    muzzleRotation = direction.ToWorldAngle() + muzzle.AngleOffset;
            }

            muzzleCoords = originCoords.Offset(muzzleRotation.RotateVec(muzzle.MuzzleOffset));
        }

        muzzlePos = _transform.ToMapCoordinates(muzzleCoords).Position;
        return true;
    }

    private bool TryGetTurretMuzzlePositions(
        EntityUid uid,
        VehicleTurretMuzzleComponent turretMuzzle,
        MapId mapId,
        out Vector2 turretBasePos,
        out Vector2 basePos,
        out Vector2 leftPos,
        out Vector2 rightPos,
        out float leftRadius,
        out float rightRadius,
        out bool useRightNext)
    {
        turretBasePos = default;
        basePos = default;
        leftPos = default;
        rightPos = default;
        leftRadius = 0f;
        rightRadius = 0f;
        useRightNext = turretMuzzle.UseRightNext;

        if (!_ents.TryGetComponent(uid, out TransformComponent? turretXform))
            return false;
        if (turretXform.MapID != mapId)
            return false;

        if (!TryGetGunOriginCoordinates(uid, mapId, out var originCoords))
            return false;

        basePos = _transform.ToMapCoordinates(originCoords).Position;
        turretBasePos = _transform.ToMapCoordinates(_transform.GetMoverCoordinates(uid)).Position;

        var turretRotation = _transform.GetWorldRotation(uid);
        var leftOffset = GetTurretOffset(turretMuzzle, turretRotation, useRight: false);
        var rightOffset = GetTurretOffset(turretMuzzle, turretRotation, useRight: true);

        if (leftOffset != Vector2.Zero)
            leftPos = basePos + turretRotation.RotateVec(leftOffset);
        else
            leftPos = basePos;

        if (rightOffset != Vector2.Zero)
            rightPos = basePos + turretRotation.RotateVec(rightOffset);
        else
            rightPos = basePos;

        leftRadius = (leftPos - turretBasePos).Length();
        rightRadius = (rightPos - turretBasePos).Length();

        return true;
    }

    private bool TryGetGunOriginCoordinates(EntityUid uid, MapId mapId, out EntityCoordinates originCoords)
    {
        originCoords = default;

        var baseUid = uid;
        if (_muzzleQ.TryComp(uid, out var muzzle) &&
            muzzle.UseContainerOwner &&
            _container.TryGetContainingContainer((uid, null), out var container))
        {
            baseUid = container.Owner;
        }

        if (!_ents.TryGetComponent(baseUid, out TransformComponent? baseXform))
            return false;
        if (baseXform.MapID != mapId)
            return false;

        var baseCoords = _transform.GetMoverCoordinates(baseUid);
        if (muzzle == null)
        {
            originCoords = baseCoords;
            return true;
        }

        var baseRotation = GetBaseRotation(baseUid, muzzle.AngleOffset);
        var (offset, rotateOffset) = GetOffset(muzzle, baseUid, baseRotation);
        var fromCoords = rotateOffset
            ? baseCoords.Offset(baseRotation.RotateVec(offset))
            : baseCoords.Offset(offset);

        if (muzzle.MuzzleOffset != Vector2.Zero)
        {
            var muzzleRotation = baseRotation;
            if (muzzle.UseAimDirection &&
                _gunQ.TryComp(uid, out var gun) &&
                gun.ShootCoordinates is { } shootCoords)
            {
                var pivotMap = _transform.ToMapCoordinates(fromCoords);
                var targetMap = _transform.ToMapCoordinates(shootCoords);
                var direction = targetMap.Position - pivotMap.Position;
                if (direction.LengthSquared() > 0.0001f)
                    muzzleRotation = direction.ToWorldAngle() + muzzle.AngleOffset;
            }

            fromCoords = fromCoords.Offset(muzzleRotation.RotateVec(muzzle.MuzzleOffset));
        }

        originCoords = fromCoords;
        return true;
    }

    private bool TryGetTurretOverlayPositions(
        EntityUid turretUid,
        VehicleTurretComponent turret,
        MapId mapId,
        out Vector2 basePos,
        out Vector2 anchorPos,
        out Vector2 turretPos)
    {
        basePos = default;
        anchorPos = default;
        turretPos = default;

        if (!TryGetVehicle(turretUid, out var vehicle))
            return false;

        var baseCoords = _transform.GetMoverCoordinates(vehicle);
        var baseMap = _transform.ToMapCoordinates(baseCoords);
        if (baseMap.MapId != mapId)
            return false;

        TryGetAnchorTurret(turretUid, turret, out var anchorUid, out var anchorTurret);

        var vehicleRot = _transform.GetWorldRotation(vehicle);
        var baseFacingAngle = GetVehicleFacingAngle(vehicle, vehicleRot);
        var anchorFacingAngle = GetOffsetFacing(anchorTurret, anchorTurret, vehicleRot, baseFacingAngle);
        var anchorLocalOffset = (-vehicleRot).RotateVec(GetPixelOffset(anchorTurret, anchorFacingAngle) / PixelsPerMeter);
        var anchorCoords = baseCoords.Offset(anchorLocalOffset);

        basePos = baseMap.Position;
        anchorPos = _transform.ToMapCoordinates(anchorCoords).Position;

        if (anchorUid == turretUid)
        {
            turretPos = anchorPos;
            return true;
        }

        var localRot = anchorTurret.RotateToCursor ? anchorTurret.WorldRotation : Angle.Zero;
        var turretFacingAngle = GetOffsetFacing(turret, anchorTurret, vehicleRot, baseFacingAngle);
        var worldOffset = GetPixelOffset(turret, turretFacingAngle) / PixelsPerMeter;
        Vector2 relativeAnchorOffset;
        Vector2 turretLocalOffset;

        if (turret.OffsetRotatesWithTurret)
        {
            if (turret.UseDirectionalOffsets)
            {
                var dir = GetDirectionalDir(turretFacingAngle);
                var snappedAngle = GetDirectionalAngle(dir);
                relativeAnchorOffset = (-snappedAngle).RotateVec(worldOffset);
                turretLocalOffset = (localRot - snappedAngle).RotateVec(worldOffset);
            }
            else
            {
                relativeAnchorOffset = worldOffset;
                turretLocalOffset = localRot.RotateVec(relativeAnchorOffset);
            }
        }
        else
        {
            turretLocalOffset = (-vehicleRot).RotateVec(worldOffset);
            relativeAnchorOffset = (-localRot).RotateVec(turretLocalOffset);
        }
        var turretCoords = new EntityCoordinates(anchorUid, relativeAnchorOffset);
        var turretMap = _transform.ToMapCoordinates(turretCoords);
        if (turretMap.MapId != mapId)
            return false;

        turretPos = turretMap.Position;
        return true;
    }

    private Vector2 GetPixelOffset(VehicleTurretComponent turret, Angle facing)
    {
        if (!turret.UseDirectionalOffsets)
            return turret.PixelOffset;

        var baseOffset = turret.PixelOffset;
        var normalized = facing.Theta % MathHelper.TwoPi;
        if (normalized < 0)
            normalized += MathHelper.TwoPi;

        var dir = GetDirectionalDir((float) normalized);
        return baseOffset + GetDirectionalOffset(turret, dir);
    }

    private static Vector2 GetDirectionalOffset(VehicleTurretComponent turret, Direction dir)
    {
        return dir switch
        {
            Direction.South => turret.PixelOffsetSouth,
            Direction.East => turret.PixelOffsetEast,
            Direction.North => turret.PixelOffsetNorth,
            Direction.West => turret.PixelOffsetWest,
            _ => Vector2.Zero
        };
    }

    private static Direction GetDirectionalDir(Angle facing)
    {
        return facing.GetCardinalDir();
    }

    private static Direction GetDirectionalDir(float normalized)
    {
        return new Angle(normalized).GetCardinalDir();
    }

    private static Angle GetDirectionalAngle(Direction dir)
    {
        return dir.ToAngle();
    }

    private Vector2 GetTurretOffset(VehicleTurretMuzzleComponent muzzle, Angle turretRotation, bool useRight)
    {
        if (!muzzle.UseDirectionalOffsets)
            return useRight ? muzzle.OffsetRight : muzzle.OffsetLeft;

        return turretRotation.GetCardinalDir() switch
        {
            Direction.North => useRight ? muzzle.OffsetRightNorth : muzzle.OffsetLeftNorth,
            Direction.East => useRight ? muzzle.OffsetRightEast : muzzle.OffsetLeftEast,
            Direction.South => useRight ? muzzle.OffsetRightSouth : muzzle.OffsetLeftSouth,
            Direction.West => useRight ? muzzle.OffsetRightWest : muzzle.OffsetLeftWest,
            _ => useRight ? muzzle.OffsetRight : muzzle.OffsetLeft
        };
    }

    private void DrawShootArc(EntityUid uid, Vector2 origin, MapId mapId, DrawingHandleWorld handle)
    {
        if (!_fireArcQ.TryComp(uid, out var arc))
            return;

        if (!_container.TryGetContainingContainer((uid, null), out var container))
            return;

        if (!_ents.TryGetComponent(container.Owner, out TransformComponent? baseXform) || baseXform.MapID != mapId)
            return;

        var baseRotation = GetBaseRotation(container.Owner, arc.AngleOffset);
        var halfArc = Angle.FromDegrees(arc.Arc.Degrees / 2f);
        var left = baseRotation + halfArc;
        var right = baseRotation - halfArc;

        const float arcLength = 3.5f;
        handle.DrawLine(origin, origin + baseRotation.ToWorldVec() * arcLength, new Color(0.2f, 0.9f, 0.3f, 0.8f));
        handle.DrawLine(origin, origin + left.ToWorldVec() * arcLength, new Color(0.95f, 0.45f, 0.2f, 0.8f));
        handle.DrawLine(origin, origin + right.ToWorldVec() * arcLength, new Color(0.95f, 0.45f, 0.2f, 0.8f));
    }

    private void DrawShootTarget(EntityUid uid, Vector2 origin, MapId mapId, DrawingHandleWorld handle)
    {
        if (!_gunQ.TryComp(uid, out var gun) || gun.ShootCoordinates == null)
            return;

        var targetCoords = _transform.ToMapCoordinates(gun.ShootCoordinates.Value);
        if (targetCoords.MapId != mapId)
            return;

        handle.DrawLine(origin, targetCoords.Position, new Color(0.9f, 0.2f, 0.9f, 0.7f));
        handle.DrawCircle(targetCoords.Position, 0.08f, new Color(0.9f, 0.2f, 0.9f, 0.8f));
    }

    private Angle GetBaseRotation(EntityUid baseUid, Angle angleOffset)
    {
        var rotation = _transform.GetWorldRotation(baseUid);
        if (_moverQ.TryComp(baseUid, out var mover) && mover.CurrentDirection != Vector2i.Zero)
            rotation = new Vector2(mover.CurrentDirection.X, mover.CurrentDirection.Y).ToWorldAngle();

        return rotation + angleOffset;
    }

    private Angle GetVehicleFacingAngle(EntityUid vehicle, Angle vehicleRot)
    {
        if (_moverQ.TryComp(vehicle, out var mover) && mover.CurrentDirection != Vector2i.Zero)
            return new Vector2(mover.CurrentDirection.X, mover.CurrentDirection.Y).ToWorldAngle();

        return vehicleRot;
    }

    private Angle GetOffsetFacing(
        VehicleTurretComponent turret,
        VehicleTurretComponent anchorTurret,
        Angle vehicleRot,
        Angle baseFacingAngle)
    {
        if (!turret.OffsetRotatesWithTurret)
            return baseFacingAngle;

        return (vehicleRot + anchorTurret.WorldRotation).Reduced();
    }

    private (Vector2 Offset, bool Rotate) GetOffset(
        GunMuzzleOffsetComponent muzzle,
        EntityUid baseUid,
        Angle baseRotation)
    {
        if (!muzzle.UseDirectionalOffsets)
            return (muzzle.Offset, true);

        var dir = GetBaseDirection(baseUid, baseRotation);
        var offset = dir switch
        {
            Direction.North => muzzle.OffsetNorth,
            Direction.East => muzzle.OffsetEast,
            Direction.South => muzzle.OffsetSouth,
            Direction.West => muzzle.OffsetWest,
            _ => muzzle.Offset,
        };

        return (offset, muzzle.RotateDirectionalOffsets);
    }

    private Direction GetBaseDirection(EntityUid baseUid, Angle baseRotation)
    {
        if (_moverQ.TryComp(baseUid, out var mover) && mover.CurrentDirection != Vector2i.Zero)
            return mover.CurrentDirection.AsDirection();

        return baseRotation.GetCardinalDir();
    }

    private bool TryGetVehicle(EntityUid turretUid, out EntityUid vehicle)
    {
        vehicle = default;
        var current = turretUid;

        while (_container.TryGetContainingContainer((current, null), out var container))
        {
            var owner = container.Owner;
            if (_ents.HasComponent<VehicleComponent>(owner))
            {
                vehicle = owner;
                return true;
            }

            current = owner;
        }

        return false;
    }

    private void TryGetAnchorTurret(
        EntityUid turretUid,
        VehicleTurretComponent turret,
        out EntityUid anchorUid,
        out VehicleTurretComponent anchorTurret)
    {
        anchorUid = turretUid;
        anchorTurret = turret;

        if (!_ents.HasComponent<VehicleTurretAttachmentComponent>(turretUid))
            return;

        if (!TryGetParentTurret(turretUid, out var parentUid, out var parentTurret))
            return;

        anchorUid = parentUid;
        anchorTurret = parentTurret;
    }

    private bool TryGetParentTurret(
        EntityUid turretUid,
        out EntityUid parentUid,
        out VehicleTurretComponent parentTurret)
    {
        parentUid = default;
        parentTurret = default!;
        var current = turretUid;

        while (_container.TryGetContainingContainer((current, null), out var container))
        {
            var owner = container.Owner;
            if (_turretQ.TryComp(owner, out var turret))
            {
                parentUid = owner;
                parentTurret = turret;
                return true;
            }

            current = owner;
        }

        return false;
    }
}
