using Content.Shared._RMC14.Projectiles.Aimed;
using Content.Shared._RMC14.Stun;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;

namespace Content.Shared._RMC14.Projectiles;

public sealed class RMCAreaDamageSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<AreaDamageComponent, ProjectileHitEvent>(OnAreaDamageProjectileHit);
    }

    /// <summary>
    ///     Apply damage to entities around the hit target.
    /// </summary>
    private void OnAreaDamageProjectileHit(Entity<AreaDamageComponent> ent, ref ProjectileHitEvent args)
    {
        var ev = new BeforeAreaDamageEvent(args.Target, args.Damage);
        RaiseLocalEvent(ent, ref ev);

        if(ev.Cancelled)
            return;

        ApplyAreaDamage(ent, args.Target, args.Damage);
    }

    /// <summary>
    ///     Apply damage to entities near a target.
    /// </summary>
    private void ApplyAreaDamage(EntityUid uid, EntityUid target, DamageSpecifier damage, AreaDamageComponent? areaDamage = null)
    {
        if (!Resolve(uid, ref areaDamage))
            return;

        // Only area damage if the initial target is a mob.
        if (areaDamage.DamageArea == 0 || !TryComp(target, out MobStateComponent? mobState))
            return;

        var nearbyEntities = _entityLookup.GetEntitiesInRange<MobStateComponent>(Transform(target).Coordinates, areaDamage.DamageArea);

        // Apply damage to all eligible entities in range
        foreach (var entity in nearbyEntities)
        {
            if(entity.Owner == target)
                continue;

            var fromCoords = _transform.GetMapCoordinates(target);
            var toCoords = _transform.GetMapCoordinates(entity);
            var distance = toCoords.Position - fromCoords.Position;
            var newDamage = damage;

            // Reduce damage if the distance is bigger than the falloff range
            if (areaDamage.FalloffRange / distance.Length() < 1)
                newDamage *= areaDamage.FalloffRange / distance.Length();

            _sizeStun.TryGetSize(entity, out var size);

            // Xenos take double area damage in CM13, I tried finding out why without success
            if (size >= RMCSizes.SmallXeno)
                newDamage *= 2;

            _damage.TryChangeDamage(entity, newDamage);
        }
    }
}

[ByRefEvent]
public record struct BeforeAreaDamageEvent(EntityUid Target, DamageSpecifier Damage,  bool Cancelled = false);
