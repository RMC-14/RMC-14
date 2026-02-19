using Content.Shared._RMC14.Explosion;
using Content.Shared.Explosion.Components;

namespace Content.Server._RMC14.Explosion;

public sealed class HefaKnightsExplosionSystem : SharedHefaKnightsExplosionSystem
{
    protected override void ExplodeSword(Entity<HefaSwordSplosionComponent> ent, EntityUid user, EntityUid target)
    {
        if (!TryComp(ent, out ExplosiveComponent? explosive))
            return;

        var userXform = Transform(user);
        var targetXform = Transform(target);
        var targetCoords = TransformSystem.GetMapCoordinates(targetXform);
        var userRotation = userXform.LocalRotation;
    }
}
