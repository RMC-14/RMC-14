using Content.Shared.Sticky;

namespace Content.Shared._RMC14.Explosion;

public sealed class RMCBreachingChargeSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCBreachingChargeComponent, AttemptEntityStickEvent>(OnAttemptStick);
        SubscribeLocalEvent<RMCBreachingChargeComponent, EntityStuckEvent>(OnStuck);
    }

    private void OnAttemptStick(Entity<RMCBreachingChargeComponent> ent, ref AttemptEntityStickEvent args)
    {
        if (!TryGetPlantingDirection(args.User, args.Target, out _))
            args.Cancelled = true;
    }

    private void OnStuck(Entity<RMCBreachingChargeComponent> ent, ref EntityStuckEvent args)
    {
        if (!TryGetPlantingDirection(args.User, args.Target, out var direction))
            return;

        _transform.SetWorldRotation(ent, direction);
        ent.Comp.DirectionSet = true;
        Dirty(ent);
    }

    private bool TryGetPlantingDirection(EntityUid user, EntityUid target, out Angle direction)
    {
        direction = default;
        var userCoordinates = _transform.GetMapCoordinates(user);
        var targetCoordinates = _transform.GetMapCoordinates(target);

        if (userCoordinates.MapId != targetCoordinates.MapId)
            return false;

        var delta = targetCoordinates.Position - userCoordinates.Position;
        if (delta.IsLengthZero())
            return false;

        direction = delta.ToWorldAngle().GetCardinalDir().ToAngle();
        return true;
    }
}
