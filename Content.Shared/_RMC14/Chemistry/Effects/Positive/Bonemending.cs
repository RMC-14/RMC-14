using Content.Shared._RMC14.Damage;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Bonemending : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Rapidly increases bone tissue regeneration, allowing broken bones to knit quickly.";
    }

    // TODO RMC14: pick random human limb each tick &, if its broken & no limb is already knitting,
    // starts knitting it: 12 min chest/groin, 10 min head, 6 min arms/legs, 3 min hands/feet, minus 15s per potency
    // (never under 5s). Unsplinted limbs count as potency -1 & dont knit at all below that

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(BruteGroup, potency * 2f);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    // TODO RMC14: instant limb fracture on critical overdose - fracture(100) on a random limb per 2s tick
    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
    }
}
