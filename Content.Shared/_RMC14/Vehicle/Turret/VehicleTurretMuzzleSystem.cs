using System.Numerics;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Shared._RMC14.Vehicle;

public sealed class VehicleTurretMuzzleSystem : EntitySystem
{
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

        args.FromCoordinates = GetMuzzleCoordinates(ent.Owner, ent.Comp, args.FromCoordinates);
    }

    private void OnGunShot(Entity<VehicleTurretMuzzleComponent> ent, ref GunShotEvent args)
    {
        if (!ent.Comp.Alternate || args.Ammo.Count == 0)
            return;

        for (var i = 0; i < args.Ammo.Count; i++)
            ent.Comp.UseRightNext = !ent.Comp.UseRightNext;

        Dirty(ent);
    }

    public EntityCoordinates GetMuzzleCoordinates(
        EntityUid uid,
        VehicleTurretMuzzleComponent muzzle,
        EntityCoordinates baseCoords)
    {
        var offset = GetWorldOffset(uid, muzzle);
        return offset == Vector2.Zero ? baseCoords : baseCoords.Offset(offset);
    }

    public Vector2 GetWorldOffset(
        EntityUid uid,
        VehicleTurretMuzzleComponent muzzle,
        bool? useRightOverride = null)
    {
        var baseRotation = _transform.GetWorldRotation(uid);
        var useRight = useRightOverride ?? (muzzle.Alternate && muzzle.UseRightNext);
        var offset = GetOffset(muzzle, baseRotation, useRight);
        return offset == Vector2.Zero ? Vector2.Zero : baseRotation.RotateVec(offset);
    }

    private Vector2 GetOffset(VehicleTurretMuzzleComponent muzzle, Angle baseRotation, bool useRight)
    {
        if (!muzzle.UseDirectionalOffsets)
        {
            var baseOffset = useRight ? muzzle.OffsetRight : muzzle.OffsetLeft;
            return baseOffset;
        }

        return VehicleTurretDirectionHelpers.GetRenderAlignedCardinalDir(baseRotation) switch
        {
            Direction.North => useRight ? muzzle.OffsetRightNorth : muzzle.OffsetLeftNorth,
            Direction.East => useRight ? muzzle.OffsetRightEast : muzzle.OffsetLeftEast,
            Direction.South => useRight ? muzzle.OffsetRightSouth : muzzle.OffsetLeftSouth,
            Direction.West => useRight ? muzzle.OffsetRightWest : muzzle.OffsetLeftWest,
            _ => useRight ? muzzle.OffsetRight : muzzle.OffsetLeft
        };
    }
}
