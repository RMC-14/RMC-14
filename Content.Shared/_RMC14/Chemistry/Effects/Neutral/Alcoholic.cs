using Content.Shared._RMC14.Drowsyness;
using Content.Shared._RMC14.Mute;
using Content.Shared.Damage;
using Content.Shared.Drunk;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Alcoholic : RMCChemicalEffect
{
    public override string Abbreviation => "AOL";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Causes the user to be drunk.\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond * 0.5}[/color] toxin damage, [color=red]{PotencyPerSecond}[/color] asphyxiation damage, and drowsiness and vomiting periodically.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond}[/color] toxin damage, [color=red]{PotencyPerSecond * 2}[/color] asphyxiation damage, and drowsiness and vomiting periodically.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = System<StatusEffectsSystem>(args);
        status.TryAddStatusEffect<DrunkComponent>(
            args.TargetEntity,
            "Drunk",
            TimeSpan.FromSeconds(5),
            true
        );
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, PoisonType, potency * 0.5);
        TryChangeDamage(args, AsphyxiationType, potency);

        if (TryComp(args, out DrowsynessComponent? drowsyness))
        {
            var drowsynessSys = System<DrowsynessSystem>(args);
            drowsynessSys.TryChange(args.TargetEntity, FixedPoint2.Min(drowsyness.Amount + potency * 2, potency * 7.5));
        }

        // TODO RMC14 sleep, confused, slurring, dizziness
        if (IsHumanoid(args) && ProbHundred(potency.Float() * 4f))
        {
            var vomitEvent = new RMCVomitEvent(args.TargetEntity);
            args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref vomitEvent);
        }
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, PoisonType, potency);
        TryChangeDamage(args, AsphyxiationType, potency * 2);

        if (TryComp(args, out DrowsynessComponent? drowsyness))
        {
            var drowsynessSys = System<DrowsynessSystem>(args);
            drowsynessSys.TryChange(args.TargetEntity, FixedPoint2.Min(drowsyness.Amount + potency * 4, potency * 10));
        }

        // TODO RMC14 sleep, confused, slurring, dizziness
        if (IsHumanoid(args) && ProbHundred(potency.Float() * 4f))
        {
            var vomitEvent = new RMCVomitEvent(args.TargetEntity);
            args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref vomitEvent);
        }

        // TODO RMC14 organ damage liver
    }
}
