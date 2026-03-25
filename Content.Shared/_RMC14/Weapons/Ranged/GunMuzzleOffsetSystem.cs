using System.Numerics;
using Content.Shared._RMC14.Emplacements;
using Content.Shared._RMC14.Vehicle;
using Content.Shared.Vehicle.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Shared._RMC14.Weapons.Ranged;

public sealed class GunMuzzleOffsetSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunMuzzleOffsetComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<GunMuzzleOffsetComponent, RMCBeforeMuzzleFlashEvent>(OnBeforeMuzzleFlash, after: new[] { typeof(MountableWeaponSystem) });
    }

    private void OnAttemptShoot(Entity<GunMuzzleOffsetComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryGetMuzzleCoordinates(ent.Owner, ent.Comp, args.ToCoordinates, out var fromCoords, out _))
            return;

        args.FromCoordinates = fromCoords;
    }

    private void OnBeforeMuzzleFlash(Entity<GunMuzzleOffsetComponent> ent, ref RMCBeforeMuzzleFlashEvent args)
    {
        if (!ent.Comp.ApplyToMuzzleFlash)
            return;

        EntityCoordinates? target = null;
        if (TryComp(ent, out GunComponent? gun))
            target = gun.ShootCoordinates;

        if (!TryGetMuzzleCoordinates(ent.Owner, ent.Comp, target, out var muzzleCoords, out _))
            return;

        var muzzleMap = _transform.ToMapCoordinates(muzzleCoords);
        var weaponMap = _transform.GetMapCoordinates(args.Weapon);
        if (muzzleMap.MapId != weaponMap.MapId)
            return;

        var worldOffset = muzzleMap.Position - weaponMap.Position;
        var weaponRotation = _transform.GetWorldRotation(args.Weapon);
        args.Offset = (-weaponRotation).RotateVec(worldOffset);
    }

    public bool TryGetMuzzleCoordinates(
        EntityUid uid,
        GunMuzzleOffsetComponent component,
        EntityCoordinates? toCoordinates,
        out EntityCoordinates muzzleCoords,
        out Angle muzzleRotation)
    {
        muzzleCoords = default;
        muzzleRotation = Angle.Zero;

        if (component.Offset == Vector2.Zero &&
            component.MuzzleOffset == Vector2.Zero &&
            !component.UseDirectionalOffsets)
        {
            return false;
        }

        var baseUid = uid;
        if (component.UseContainerOwner &&
            _container.TryGetContainingContainer((uid, null), out var container))
        {
            baseUid = container.Owner;
        }

        var baseCoords = _transform.GetMoverCoordinates(baseUid);
        var baseRotation = GetBaseRotation(baseUid, component.AngleOffset);
        var (offset, rotateOffset) = GetOffset(component, baseUid, baseRotation);
        muzzleCoords = rotateOffset
            ? baseCoords.Offset(baseRotation.RotateVec(offset))
            : baseCoords.Offset(offset);
        muzzleRotation = baseRotation;

        if (component.MuzzleOffset == Vector2.Zero)
            return true;

        if (component.UseAimDirection && toCoordinates != null)
        {
            var pivotMap = _transform.ToMapCoordinates(muzzleCoords);
            var targetMap = _transform.ToMapCoordinates(toCoordinates.Value);
            if (pivotMap.MapId == targetMap.MapId)
            {
                var direction = targetMap.Position - pivotMap.Position;
                if (direction.LengthSquared() > 0.0001f)
                    muzzleRotation = direction.ToWorldAngle() + component.AngleOffset;
            }
        }

        muzzleCoords = muzzleCoords.Offset(muzzleRotation.RotateVec(component.MuzzleOffset));
        return true;
    }

    private Angle GetBaseRotation(EntityUid baseUid, Angle angleOffset)
    {
        var rotation = _transform.GetWorldRotation(baseUid);
        if (TryComp(baseUid, out GridVehicleMoverComponent? mover) && mover.CurrentDirection != Vector2i.Zero)
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

        return (offset, muzzle.RotateDirectionalOffsets);
    }

    private Direction GetBaseDirection(EntityUid baseUid, Angle baseRotation)
    {
        if (TryComp(baseUid, out GridVehicleMoverComponent? mover) && mover.CurrentDirection != Vector2i.Zero)
            return mover.CurrentDirection.AsDirection();

        return VehicleTurretDirectionHelpers.GetRenderAlignedCardinalDir(baseRotation);
    }
}
