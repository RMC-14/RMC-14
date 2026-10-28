using Content.Shared._RMC14.Chemistry.Addiction;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Antiaddictive : RMCChemicalEffect
{
    private static readonly EntProtoId<StatusEffectComponent> Hallucination = "RMCStatusEffectHallucination";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Stops all bodily cravings towards addictive chemical substances.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var addiction = args.EntityManager.System<RMCAddictionSystem>();
        addiction.TryReduceAddictions(args.TargetEntity, (float) potency);
    }

    // TODO RMC14: brain damage on overdose - potency * delta_time (2 * potency per 2s tick)

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        status.TryUpdateStatusEffectDuration(args.TargetEntity, Hallucination, TimeSpan.FromSeconds(ActualPotency));
    }
}
