using Content.Shared._RMC14.Chemistry.Disabilities;
using Content.Shared._RMC14.Damage;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Painkilling : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";

    private static readonly EntProtoId<StatusEffectComponent> Hallucination = "RMCStatusEffectHallucination";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Binds to opioid receptors in the brain and spinal cord, reducing pain.";
    }

    private static FixedPoint2 EffectivePotency(FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        return args.EntityManager.HasComponent<RMCOpiateReceptorDeficiencyComponent>(args.TargetEntity)
            ? potency * 0.25f
            : potency;
    }

    // TODO RMC14: pain reduction - pain reduction of 40 * effective_potency, overdose too (only raises the
    // current reduction, at most once per 10s), reset to 0 when reagent leaves
    // Reduction loses 0.25 per point of current pain, out of 100

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var effectivePotency = EffectivePotency(potency, args);

        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        var floor = (float) EffectivePotency(ActualPotency, args) * 2f;
        status.TryUpdateStatusEffectDuration(args.TargetEntity, Hallucination, TimeSpan.FromSeconds(floor));

        var damage = args.EntityManager.System<SharedRMCDamageableSystem>().DistributeFreshDamage(ToxinGroup, effectivePotency * 2f);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[AsphyxiationType] = 1.5f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);

        // TODO RMC14: liver damage, brain damage - 6 * effective_potency liver damage and 2 * effective_potency brain damage per 2s tick
    }
}
