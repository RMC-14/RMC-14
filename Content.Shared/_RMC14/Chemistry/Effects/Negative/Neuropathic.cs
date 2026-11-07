using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Neuropathic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> BluntType = "Blunt";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Activates the somatosensory system, causing neuropathic pain throughout the body.";
    }

    // TODO RMC14: neuropathic pain - +10 * potency pain per 2s tick,
    // +20 * potency more while overdosed
    // builds up until reagent leaves, then pain is recalculated from damage

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = potency * 2f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
