using Content.Server.Explosion.EntitySystems;
using Content.Shared._RMC14.Explosion;
using Content.Shared.Explosion.Components;

namespace Content.Server._RMC14.Explosion;

public sealed class RMCBreachingChargeTriggerSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCBreachingChargeComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<RMCBreachingChargeComponent> ent, ref TriggerEvent args)
    {
        if (args.Handled || !ent.Comp.DirectionSet || !TryComp(ent, out ExplosiveComponent? explosive))
            return;

        var throwDirection = _transform.GetWorldRotation(ent).ToWorldVec();
        _explosion.TriggerExplosive(ent, explosive, user: args.User, throwDirection: throwDirection);
        args.Handled = true;
    }
}
