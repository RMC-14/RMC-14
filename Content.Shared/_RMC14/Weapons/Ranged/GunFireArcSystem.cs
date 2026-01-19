using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Weapons.Ranged;

public sealed class GunFireArcSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunFireArcComponent, AttemptShootEvent>(OnAttemptShoot);
    }

    private void OnAttemptShoot(Entity<GunFireArcComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.ToCoordinates == null)
            return;

        var fromMap = _transform.ToMapCoordinates(args.FromCoordinates);
        var toMap = _transform.ToMapCoordinates(args.ToCoordinates.Value);
        var direction = toMap.Position - fromMap.Position;

        if (direction.LengthSquared() <= 0.0001f)
            return;

        var aimAngle = direction.ToWorldAngle();

        var facing = _transform.GetWorldRotation(ent.Owner);
        if (_container.TryGetContainingContainer((ent.Owner, null), out var container))
            facing = _transform.GetWorldRotation(container.Owner);

        var center = facing + ent.Comp.AngleOffset;
        var halfArc = Angle.FromDegrees(ent.Comp.Arc.Degrees / 2f);
        var diff = Angle.ShortestDistance(aimAngle, center);

        if (diff <= halfArc && diff >= -halfArc)
            return;

        args.Cancelled = true;
        args.ResetCooldown = true;
        args.Message = Loc.GetString("rmc-gun-arc-blocked");
    }
}
