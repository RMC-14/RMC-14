using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Weapons.Ranged.Prediction;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Projectiles;

public sealed class RMCAreaDamageSystem : EntitySystem
{
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCAreaDamageComponent, ProjectileHitEvent>(OnAreaDamageProjectileHit);
    }

    /// <summary>
    ///     Apply damage to entities around the hit target.
    /// </summary>
    private void OnAreaDamageProjectileHit(Entity<RMCAreaDamageComponent> ent, ref ProjectileHitEvent args)
    {
        var ev = new BeforeAreaDamageEvent(args.Target, args.Damage);
        RaiseLocalEvent(ent, ref ev);

        if(ev.Cancelled)
            return;

        ApplyAreaDamage(ent, args.Target, args.Damage, args.Shooter);
    }

    /// <summary>
    ///     Apply damage to entities near a target.
    /// </summary>
    private void ApplyAreaDamage(EntityUid uid, EntityUid target, DamageSpecifier damage, EntityUid? shooter = null, RMCAreaDamageComponent? areaDamage = null)
    {
        if (!Resolve(uid, ref areaDamage))
            return;

        // Only area damage if the initial target is a mob.
        if (areaDamage.DamageArea == 0 || !TryComp(target, out MobStateComponent? mobState))
            return;

        var nearbyEntities = _entityLookup.GetEntitiesInRange<MobStateComponent>(Transform(target).Coordinates, areaDamage.DamageArea, LookupFlags.Uncontained);

        // Apply damage to all eligible entities in range.
        foreach (var entity in nearbyEntities)
        {
            if (entity.Owner == target || entity == shooter)
                continue;

            var fromCoords = _transform.GetMapCoordinates(target);
            var toCoords = _transform.GetMapCoordinates(entity);
            var distance = toCoords.Position - fromCoords.Position;
            var newDamage = damage;
            var armorPiercing = 0;

            // Reduce damage if the distance is bigger than the falloff range
            if (areaDamage.FalloffDistance / distance.Length() < 1)
                newDamage *= areaDamage.FalloffDistance / distance.Length();

            _sizeStun.TryGetSize(entity, out var size);

            if (TryComp(uid, out CMArmorPiercingComponent? piercing))
                armorPiercing = piercing.Amount;

            if (size >= RMCSizes.SmallXeno)
                newDamage *= areaDamage.XenoDamageMultiplier;

            var damageDealt = _damage.TryChangeDamage(entity, newDamage, armorPiercing: armorPiercing);

            if (!(damageDealt?.GetTotal() > FixedPoint2.Zero))
                continue;

            if (HasComp<PredictedProjectileClientComponent>(uid))
            {
                if (_player.LocalEntity != shooter)
                    continue;

                _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { entity }, Filter.Local());
                continue;
            }

            if (_net.IsServer)
            {
                var filter = Filter.Pvs(entity, entityManager: EntityManager)
                    .RemoveWhereAttachedEntity(attached => attached == entity.Owner);

                if (HasComp<PredictedProjectileServerComponent>(uid))
                    filter = filter.RemoveWhereAttachedEntity(attached => attached == shooter);

                _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { entity }, filter);
            }
        }
    }
}

[ByRefEvent]
public record struct BeforeAreaDamageEvent(EntityUid Target, DamageSpecifier Damage,  bool Cancelled = false);
