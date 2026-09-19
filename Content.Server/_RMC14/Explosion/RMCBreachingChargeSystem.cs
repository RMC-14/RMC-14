using Content.Server.Explosion.EntitySystems;
using Content.Shared._RMC14.Explosion;
using Content.Shared.Explosion.Components;
using Content.Shared.Sticky;
using Robust.Server.GameObjects;

namespace Content.Server._RMC14.Explosion;

public sealed class RMCBreachingChargeSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCBreachingChargeComponent, AttemptEntityStickEvent>(OnAttemptStick);
        SubscribeLocalEvent<RMCBreachingChargeComponent, EntityStuckEvent>(OnStuck);
        SubscribeLocalEvent<RMCBreachingChargeComponent, TriggerEvent>(OnTrigger);
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
    }

    private void OnTrigger(Entity<RMCBreachingChargeComponent> ent, ref TriggerEvent args)
    {
        if (!ent.Comp.DirectionSet || !TryComp(ent, out ExplosiveComponent? explosive))
            return;

        var throwDirection = _transform.GetWorldRotation(ent).ToWorldVec();
        _explosion.TriggerExplosive(ent, explosive, user: args.User, throwDirection: throwDirection);
        args.Handled = true;
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

        direction = delta.ToWorldAngle().GetDir().ToAngle();
        return true;
    }
}
