using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Stun;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed class ProjectileStunRemoveSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ProjectileStunRemoveComponent, AmmoShotEvent>(RemoveBulletStun);
    }
    private void RemoveBulletStun(Entity<ProjectileStunRemoveComponent> ent, ref AmmoShotEvent args)
    {
        foreach (var projectile in args.FiredProjectiles)
        {
            RemComp<RMCStunOnHitComponent>(projectile);
        }
    }
}
