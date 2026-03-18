using System.Numerics;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Shared._RMC14.Vehicle;

public sealed class VehicleTurretMuzzleSystem : EntitySystem
{
    [Dependency] private readonly RMCVehicleTopologySystem _topology = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleTurretMuzzleComponent, AttemptShootEvent>(OnAttemptShoot, after: new[] { typeof(GunMuzzleOffsetSystem) });
        SubscribeLocalEvent<VehicleTurretMuzzleComponent, GunShotEvent>(OnGunShot);
    }

    private void OnAttemptShoot(Entity<VehicleTurretMuzzleComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;

        var baseCoords = args.FromCoordinates;
        var baseRotation = _transform.GetWorldRotation(ent.Owner);
        var useRight = ent.Comp.Alternate && ent.Comp.UseRightNext;
        var (offset, rotateOffset) = GetOffset(ent, baseRotation, useRight);
        if (offset == Vector2.Zero)
            return;

        args.FromCoordinates = rotateOffset
            ? baseCoords.Offset(baseRotation.RotateVec(offset))
            : baseCoords.Offset(offset);
    }

    private void OnGunShot(Entity<VehicleTurretMuzzleComponent> ent, ref GunShotEvent args)
    {
        if (!ent.Comp.Alternate || args.Ammo.Count == 0)
            return;

        for (var i = 0; i < args.Ammo.Count; i++)
            ent.Comp.UseRightNext = !ent.Comp.UseRightNext;

        Dirty(ent);
    }

    private (Vector2 Offset, bool Rotate) GetOffset(
        Entity<VehicleTurretMuzzleComponent> ent,
        Angle baseRotation,
        bool useRight)
    {
        var muzzle = ent.Comp;
        if (!muzzle.UseDirectionalOffsets)
        {
            var baseOffset = useRight ? muzzle.OffsetRight : muzzle.OffsetLeft;
            return (baseOffset, true);
        }

        var dir = TryGetTurretLocalDirection(ent.Owner, out var turretDir)
            ? turretDir
            : VehicleTurretDirectionHelpers.GetRenderAlignedCardinalDir(baseRotation);

        var offset = dir switch
        {
            Direction.North => useRight ? muzzle.OffsetRightNorth : muzzle.OffsetLeftNorth,
            Direction.East => useRight ? muzzle.OffsetRightEast : muzzle.OffsetLeftEast,
            Direction.South => useRight ? muzzle.OffsetRightSouth : muzzle.OffsetLeftSouth,
            Direction.West => useRight ? muzzle.OffsetRightWest : muzzle.OffsetLeftWest,
            _ => useRight ? muzzle.OffsetRight : muzzle.OffsetLeft
        };

        return (offset, true);
    }

    private bool TryGetTurretLocalDirection(EntityUid turretUid, out Direction dir)
    {
        dir = default;

        if (!TryComp(turretUid, out VehicleTurretComponent? turret) ||
            !turret.OffsetRotatesWithTurret)
        {
            return false;
        }

        TryGetAnchorTurret(turretUid, turret, out _, out var anchorTurret);
        var localRotation = anchorTurret.RotateToCursor ? anchorTurret.WorldRotation : Angle.Zero;
        dir = VehicleTurretDirectionHelpers.GetRenderAlignedCardinalDir(localRotation);
        return true;
    }

    private void TryGetAnchorTurret(
        EntityUid turretUid,
        VehicleTurretComponent turret,
        out EntityUid anchorUid,
        out VehicleTurretComponent anchorTurret)
    {
        anchorUid = turretUid;
        anchorTurret = turret;

        if (!HasComp<VehicleTurretAttachmentComponent>(turretUid))
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
        if (!_topology.TryGetParentTurret(turretUid, out parentUid) ||
            !TryComp(parentUid, out VehicleTurretComponent? resolvedTurret))
        {
            return false;
        }

        parentTurret = resolvedTurret;
        return true;
    }

}
