using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Standing;
using Robust.Shared.Physics.Events;
using Robust.Shared.Containers;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Robust.Shared.Map;

namespace Content.Shared.Damage.Components;

public sealed class RequireProjectileTargetSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RequireProjectileTargetComponent, PreventCollideEvent>(PreventCollide);
        SubscribeLocalEvent<RequireProjectileTargetComponent, StoodEvent>(StandingBulletHit);
        SubscribeLocalEvent<RequireProjectileTargetComponent, DownedEvent>(LayingBulletPass);
    }

    private void PreventCollide(Entity<RequireProjectileTargetComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
          return;

        if (!ent.Comp.Active)
            return;

        var other = args.OtherEntity;
        if (TryComp(other, out ProjectileComponent? projectile) &&
            CompOrNull<TargetedProjectileComponent>(other) is TargetedProjectileComponent targetedProjectileComp)
        {
            if (ent.Comp.CollideOnTargetCoords && targetedProjectileComp.TargetCoordinates is EntityCoordinates)
            {
                // If requireProjectileComponent check target coords and the target coords intersect with this entity, allow the collision.
                if (_lookup.GetEntitiesIntersecting(targetedProjectileComp.TargetCoordinates.Value).Contains(ent.Owner))
                {
                    return;
                }
            }
            else
            {
                // Otherwise, if target is this entity, allow the collision.
                if (targetedProjectileComp.Target == ent.Owner)
                {
                    return;
                }
            }



            // Prevents shooting out of while inside of crates
            var shooter = projectile.Shooter;
            if (!shooter.HasValue)
                return;

            // ProjectileGrenades delete the entity that's shooting the projectile,
            // so it's impossible to check if the entity is in a container
            if (TerminatingOrDeleted(shooter.Value))
                return;

            if (!_container.IsEntityOrParentInContainer(shooter.Value))
               args.Cancelled = true;
        }
    }

    private void SetActive(Entity<RequireProjectileTargetComponent> ent, bool value)
    {
        if (ent.Comp.Active == value)
            return;

        ent.Comp.Active = value;
        Dirty(ent);
    }

    private void StandingBulletHit(Entity<RequireProjectileTargetComponent> ent, ref StoodEvent args)
    {
        SetActive(ent, false);
    }

    private void LayingBulletPass(Entity<RequireProjectileTargetComponent> ent, ref DownedEvent args)
    {
        SetActive(ent, true);
    }
}
