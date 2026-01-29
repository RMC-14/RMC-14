using System.Numerics;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameObjects;
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

        var baseCoords = args.FromCoordinates;
        var baseRotation = _transform.GetWorldRotation(ent.Owner);
        var useRight = ent.Comp.Alternate && ent.Comp.UseRightNext;
        var offset = GetOffset(ent.Comp, baseRotation, useRight);
        if (offset == Vector2.Zero)
            return;

        args.FromCoordinates = baseCoords.Offset(baseRotation.RotateVec(offset));
    }

    private void OnGunShot(Entity<VehicleTurretMuzzleComponent> ent, ref GunShotEvent args)
    {
        if (!ent.Comp.Alternate || args.Ammo.Count == 0)
            return;

        for (var i = 0; i < args.Ammo.Count; i++)
            ent.Comp.UseRightNext = !ent.Comp.UseRightNext;

        Dirty(ent);
    }

    private Vector2 GetOffset(VehicleTurretMuzzleComponent muzzle, Angle baseRotation, bool useRight)
    {
        if (!muzzle.UseDirectionalOffsets)
        {
            var baseOffset = useRight ? muzzle.OffsetRight : muzzle.OffsetLeft;
            return baseOffset;
        }

        return baseRotation.GetCardinalDir() switch
        {
            Direction.North => useRight ? muzzle.OffsetRightNorth : muzzle.OffsetLeftNorth,
            Direction.East => useRight ? muzzle.OffsetRightEast : muzzle.OffsetLeftEast,
            Direction.South => useRight ? muzzle.OffsetRightSouth : muzzle.OffsetLeftSouth,
            Direction.West => useRight ? muzzle.OffsetRightWest : muzzle.OffsetLeftWest,
            _ => useRight ? muzzle.OffsetRight : muzzle.OffsetLeft
        };
    }
}
