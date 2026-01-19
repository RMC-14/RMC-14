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

    public bool Enabled { get; set; }

    private readonly IEntityManager _ents;
    private readonly SharedTransformSystem _transform;
    private readonly SharedContainerSystem _container;
    private readonly EntityQuery<GunComponent> _gunQ;
    private readonly EntityQuery<GunFireArcComponent> _fireArcQ;
    private readonly EntityQuery<GridVehicleMoverComponent> _moverQ;
    private readonly EntityQuery<VehicleTurretComponent> _turretQ;
    private readonly EntityQuery<VehiclePortGunComponent> _portGunQ;

    public VehicleHardpointDebugOverlay(IEntityManager ents)
    {
        _ents = ents;
        _transform = ents.System<SharedTransformSystem>();
        _container = ents.System<SharedContainerSystem>();
        _gunQ = ents.GetEntityQuery<GunComponent>();
        _fireArcQ = ents.GetEntityQuery<GunFireArcComponent>();
        _moverQ = ents.GetEntityQuery<GridVehicleMoverComponent>();
        _turretQ = ents.GetEntityQuery<VehicleTurretComponent>();
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

        return (offset, false);
    }

    private Direction GetBaseDirection(EntityUid baseUid, Angle baseRotation)
    {
        if (_moverQ.TryComp(baseUid, out var mover) && mover.CurrentDirection != Vector2i.Zero)
            return mover.CurrentDirection.AsDirection();

        return baseRotation.GetCardinalDir();
    }
}
