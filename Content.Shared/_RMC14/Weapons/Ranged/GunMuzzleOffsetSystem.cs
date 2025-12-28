using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using System.Numerics;

namespace Content.Shared._RMC14.Weapons.Ranged;

public sealed class GunMuzzleOffsetSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunMuzzleOffsetComponent, AttemptShootEvent>(OnAttemptShoot);
    }

    private void OnAttemptShoot(Entity<GunMuzzleOffsetComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.Offset == Vector2.Zero && ent.Comp.MuzzleOffset == Vector2.Zero)
            return;

        var baseUid = ent.Owner;
        if (ent.Comp.UseContainerOwner &&
            _container.TryGetContainingContainer((ent.Owner, null), out var container))
        {
            baseUid = container.Owner;
        }

        var baseCoords = _transform.GetMoverCoordinates(baseUid);
        var baseRotation = _transform.GetWorldRotation(baseUid) + ent.Comp.AngleOffset;
        var fromCoords = baseCoords.Offset(baseRotation.RotateVec(ent.Comp.Offset));

        if (ent.Comp.MuzzleOffset != Vector2.Zero)
        {
            var muzzleRotation = baseRotation;
            if (ent.Comp.UseAimDirection && args.ToCoordinates != null)
            {
                var pivotMap = _transform.ToMapCoordinates(fromCoords);
                var targetMap = _transform.ToMapCoordinates(args.ToCoordinates.Value);
                var direction = targetMap.Position - pivotMap.Position;
                if (direction.LengthSquared() > 0.0001f)
                    muzzleRotation = direction.ToWorldAngle() + ent.Comp.AngleOffset;
            }

            fromCoords = fromCoords.Offset(muzzleRotation.RotateVec(ent.Comp.MuzzleOffset));
        }

        args.FromCoordinates = fromCoords;
    }
}
