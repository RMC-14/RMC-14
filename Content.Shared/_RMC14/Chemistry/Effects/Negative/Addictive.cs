using Content.Shared._RMC14.Chemistry.Addiction;
using Content.Shared._RMC14.Chemistry.Disabilities;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Addictive : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes addiction on the very first exposure. Higher potency causes faster progression and worse withdrawal, not a higher chance of it happening.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.Reagent == null)
            return;

        var addiction = args.EntityManager.System<RMCAddictionSystem>();
        addiction.TryExposeToAddictive(args.TargetEntity, args.Reagent.ID, ActualPotency);
    }

    // TODO RMC14: brain damage on overdose - 0.5 * potency * delta_time (potency per 2s tick)

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        args.EntityManager.EnsureComponent<RMCNervousComponent>(args.TargetEntity);
    }
}
