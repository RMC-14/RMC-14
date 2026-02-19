using Content.Server.Explosion.EntitySystems;
using Content.Shared._RMC14.Explosion;

namespace Content.Server._RMC14.Explosion;

public sealed class HefaSwordTriggerOnHitSystem : SharedHefaSwordTriggerOnHitSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    protected override void TriggerSword(Entity<HefaSwordTriggerOnHitComponent> ent, EntityUid user, EntityUid target)
    {
        var targetCoords = Transform(target).Coordinates;
        _transform.SetCoordinates(ent, targetCoords);

        var userRotation = _transform.GetWorldRotation(user);
        _transform.SetWorldRotation(ent, userRotation);
        _trigger.Trigger(ent, user);
    }
}
