using System.Numerics;
using Content.Shared.Vehicle.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Maths;

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

        if (ent.Comp.Offset == Vector2.Zero && ent.Comp.MuzzleOffset == Vector2.Zero && !ent.Comp.UseDirectionalOffsets)
            return;

        var baseUid = ent.Owner;
        EntityUid? containerOwner = null;
        if (ent.Comp.UseContainerOwner &&
            _container.TryGetContainingContainer((ent.Owner, null), out var container))
        {
            baseUid = container.Owner;
            containerOwner = container.Owner;
        }

        var baseCoords = _transform.GetMoverCoordinates(baseUid);
        var baseRotation = GetBaseRotation(baseUid, ent.Comp.AngleOffset);
        var (offset, rotateOffset) = GetOffset(ent.Comp, baseUid, baseRotation);
        var fromCoords = rotateOffset
            ? baseCoords.Offset(baseRotation.RotateVec(offset))
            : baseCoords.Offset(offset);
        var muzzleRotation = baseRotation;
        if (ent.Comp.MuzzleOffset != Vector2.Zero)
        {
            if (ent.Comp.UseAimDirection && args.ToCoordinates != null)
            {
                var pivotMap = _transform.ToMapCoordinates(fromCoords);
                var targetMap = _transform.ToMapCoordinates(args.ToCoordinates.Value);
                var direction = targetMap.Position - pivotMap.Position;
                if (direction.LengthSquared() > 0.0001f)
                {
                    muzzleRotation = direction.ToWorldAngle() + ent.Comp.AngleOffset;
                }
            }

            fromCoords = fromCoords.Offset(muzzleRotation.RotateVec(ent.Comp.MuzzleOffset));
        }

        args.FromCoordinates = fromCoords;
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

        return baseRotation.GetCardinalDir();
    }
}
