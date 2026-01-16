using Content.Server.Explosion.EntitySystems;
using Content.Shared._RMC14.Explosion;

namespace Content.Server._RMC14.Explosion;

public sealed class HefaSwordSplosionSystem : SharedHefaSwordSplosionSystem
{
    [Dependency] private readonly RMCExplosionSystem _rmcExplosion = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    protected override void ExplodeSword(Entity<HefaSwordSplosionComponent> ent, EntityUid user, EntityUid target)
    {
        // Move sword to target so all effects spawn at target location.
        TransformSystem.SetCoordinates(ent, Transform(target).Coordinates);

        // Set rotation to match user's facing direction so shrapnel launches correctly.
        TransformSystem.SetLocalRotation(ent, Transform(user).LocalRotation);

        // Trigger ProjectileGrenadeComponent for shrapnel.
        _trigger.Trigger(ent, user);
        _rmcExplosion.TriggerExplosive(ent, user: user);
    }
}
