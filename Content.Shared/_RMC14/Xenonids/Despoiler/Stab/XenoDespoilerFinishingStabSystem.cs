using Content.Shared._RMC14.Xenonids.Despoiler.Acid;
using Content.Shared._RMC14.Xenonids.Stab;
using Content.Shared._RMC14.Xenonids.Projectile.Spit.Charge;
using Content.Shared.Damage;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._RMC14.Xenonids.Despoiler.Stab;

public sealed class XenoDespoilerFinishingStabSystem : EntitySystem
{
    [Dependency] private readonly XenoDespoilerAcidSystem _acid = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    private EntityQuery<UserAcidedComponent> _acidQuery;

    public override void Initialize()
    {
        _acidQuery = GetEntityQuery<UserAcidedComponent>();

        SubscribeLocalEvent<XenoDespoilerComponent, RMCGetTailStabBonusDamageEvent>(OnGetTailStabBonus);
        SubscribeLocalEvent<XenoDespoilerComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnGetTailStabBonus(Entity<XenoDespoilerComponent> ent, ref RMCGetTailStabBonusDamageEvent args)
    {
        EnsureComp<XenoDespoilerTailStabPendingComponent>(ent);
    }

    private void OnMeleeHit(Entity<XenoDespoilerComponent> ent, ref MeleeHitEvent args)
    {
        if (!RemComp<XenoDespoilerTailStabPendingComponent>(ent))
            return;

        var table = ent.Comp.FinishingStabBonusByTier;
        if (table.Count == 0)
            return;

        foreach (var target in args.HitEntities)
        {
            if (!_acidQuery.HasComp(target))
                continue;

            var tier = _acid.ConsumeAcidTier(target);
            if (tier <= 0)
                continue;

            var idx = Math.Clamp(tier - 1, 0, table.Count - 1);
            var bonus = table[idx];
            if (bonus.GetTotal() <= 0)
                continue;

            _damageable.TryChangeDamage(target, new DamageSpecifier(bonus), ignoreResistances: true, origin: ent);
        }
    }
}
