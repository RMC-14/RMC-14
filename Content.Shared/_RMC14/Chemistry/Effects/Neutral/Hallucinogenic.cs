using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Jittering;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Hallucinogenic : RMCChemicalEffect
{
    public override string Abbreviation => "HLG";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        // TODO RMC14 emotes
        // TODO RMC14 hallucination
        // TODO RMC14 druggy
        return $"Causes jitteriness.\n" +
               $"Overdoses cause jitteriness.\n" +
               $"Critical overdoses cause [color=red]2[/color] seconds of paralysis.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 emotes
        // TODO RMC14 hallucination
        // TODO RMC14 druggy
        var status = System<StatusEffectsSystem>(args);
        status.TryAddStatusEffect<JitteringComponent>(
            args.TargetEntity,
            "Jitter",
            TimeSpan.FromSeconds(5),
            true
        );
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 random step
        // TODO RMC14 hallucination
        var status = System<StatusEffectsSystem>(args);
        status.TryAddStatusEffect<JitteringComponent>(
            args.TargetEntity,
            "Jitter",
            TimeSpan.FromSeconds(5),
            true
        );
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 organ damage brain
        var stun = System<SharedStunSystem>(args);
        stun.TryParalyze(args.TargetEntity, TimeSpan.FromSeconds(2), true);
    }
}
