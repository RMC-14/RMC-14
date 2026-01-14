using Content.Server.Explosion.EntitySystems;
using Content.Shared._RMC14.Explosion;

namespace Content.Server._RMC14.Explosion;

public sealed class HefaSwordSplosionSystem : SharedHefaSwordSplosionSystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void ExplodeSword(Entity<HefaSwordOnHitTriggerComponent> ent, EntityUid user, EntityUid target)
    {
        // Move the sword to the target's position so it explodes there.
        var targetCoords = Transform(target).Coordinates;
        _transform.SetCoordinates(ent, targetCoords);

        // Set sword rotation to user's facing direction for directional shrapnel.
        var userRotation = _transform.GetWorldRotation(user);
        _transform.SetWorldRotation(ent, userRotation);
        _trigger.Trigger(ent, user);

        QueueDel(ent);
    }
}
