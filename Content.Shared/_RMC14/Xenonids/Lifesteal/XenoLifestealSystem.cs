using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Marines;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._RMC14.Xenonids.Lifesteal;

public sealed class XenoLifestealSystem : EntitySystem
{
    [Dependency] private readonly CMDamageableSystem _cmDamageable = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    private readonly HashSet<Entity<MarineComponent>> _targets = new();

    private EntityQuery<DamageableComponent> _damageableQuery;
    private EntityQuery<MarineComponent> _marineQuery;

    public override void Initialize()
    {
        _damageableQuery = GetEntityQuery<DamageableComponent>();
        _marineQuery = GetEntityQuery<MarineComponent>();

        SubscribeLocalEvent<XenoLifestealComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<XenoLifestealComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        var found = false;
        foreach (var hit in args.HitEntities)
        {
            if (!_marineQuery.HasComp(hit))
                continue;

            found = true;
            break;
        }

        if (!found)
            return;

        if (!_damageableQuery.TryComp(ent, out var damageable))
            return;

        var total = damageable.TotalDamage;
        if (total == FixedPoint2.Zero)
            return;

        _targets.Clear();
        _entityLookup.GetEntitiesInRange(ent.Owner.ToCoordinates(), ent.Comp.TargetRange, _targets);

        var lifesteal = ent.Comp.BasePercentage;
        foreach (var hit in _targets)
        {
            if (!_marineQuery.HasComp(hit))
                continue;

            lifesteal += ent.Comp.TargetIncreasePercentage;
            if (lifesteal >= ent.Comp.MaxPercentage)
            {
                lifesteal = ent.Comp.MaxPercentage;
                break;
            }
        }

        var amount = -FixedPoint2.Clamp(total * lifesteal, ent.Comp.MinHeal, ent.Comp.MaxHeal);
        var heal = _cmDamageable.DistributeTypes(ent.Owner, amount);
        _damageable.TryChangeDamage(ent, heal, true);
    }
}
