using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Stun;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed class ProjectileStunRemoveSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ProjectileStunRemoveComponent, AmmoShotEvent>(ProjectileStunRemove);
        SubscribeLocalEvent<GrantProjectileStunRemoveComponent, AttachableAlteredEvent>(CheckProjectileStunRemove);
    }
    private void ProjectileStunRemove(Entity<ProjectileStunRemoveComponent> ent, ref AmmoShotEvent args)
    {
        foreach (var projectile in args.FiredProjectiles)
        {
            RemComp<RMCStunOnHitComponent>(projectile);
        }
    }

    private void CheckProjectileStunRemove(Entity<GrantProjectileStunRemoveComponent> ent, ref AttachableAlteredEvent args)
    {
        switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                EnsureComp<ProjectileStunRemoveComponent>(args.Holder);
                break;
            case AttachableAlteredType.Detached:
                RemComp<ProjectileStunRemoveComponent>(args.Holder);
                break;
        }
    }
}
