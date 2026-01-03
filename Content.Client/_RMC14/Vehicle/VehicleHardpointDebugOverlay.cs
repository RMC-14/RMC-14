using System.Numerics;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.Weapons.Ranged;
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
    private readonly EntityQuery<VehicleTurretComponent> _turretQ;
    private readonly EntityQuery<VehiclePortGunComponent> _portGunQ;

    public VehicleHardpointDebugOverlay(IEntityManager ents)
    {
        _ents = ents;
        _transform = ents.System<SharedTransformSystem>();
        _container = ents.System<SharedContainerSystem>();
        _gunQ = ents.GetEntityQuery<GunComponent>();
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
        var baseRotation = _transform.GetWorldRotation(baseUid) + muzzle.AngleOffset;

        var originCoords = baseCoords.Offset(baseRotation.RotateVec(muzzle.Offset));
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
}
