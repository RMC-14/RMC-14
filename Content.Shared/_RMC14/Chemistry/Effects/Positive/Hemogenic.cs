using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared._RMC14.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Hemogenic : RMCChemicalEffect
{
    public override string Abbreviation => "HMG";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var baseText = $"Restores [color=green]{PotencyPerSecond}[/color]cl of blood while not hungry.\n" +
                       $"Causes [color=red]{PotencyPerSecond}[/color] nutrient loss per second.\n" +
                       $"Overdoses cause [color=red]{PotencyPerSecond}[/color] toxin damage.\n" +
                       $"Critical overdoses cause [color=red]{PotencyPerSecond * 5}[/color] additional nutrient loss";

        return Potency > 3
            ? $"Deals [color=red]{PotencyPerSecond}[/color] brute, [color=red]{PotencyPerSecond * 2}[/color] airloss damage, and slows you down.\n{baseText}"
            : baseText;
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var hunger = System<HungerSystem>(args);
        if (!TryComp<HungerComponent>(args, out var hungerComponent) || hunger.GetHunger(hungerComponent) < 200)
            return;

        hunger.ModifyHunger(args.TargetEntity, -PotencyPerSecond); // TODO RMC14 No hunger drain for Yuatjas.

        if (TryComp<BloodstreamComponent>(args, out var bloodstreamComponent))
        {
            var bloodstream = System<SharedBloodstreamSystem>(args);
            bloodstream.TryModifyBloodLevel((args.TargetEntity, bloodstreamComponent), potency);
        }

        var rmcBloodstream = System<SharedRMCBloodstreamSystem>(args);
        if (Potency > 3 &&
            rmcBloodstream.TryGetBloodSolution(args.TargetEntity, out var bloodSolution) &&
            bloodSolution.Volume > 570) // TODO RMC14 Also check if they're not a Yautja.
        {
            TryChangeDamage(args, BluntType, potency);
            TryChangeDamage(args, AsphyxiationType, potency * 2);
            // TODO RMC14 M.reagent_move_delay_modifier += potency
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, PoisonType, potency);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var hunger = System<HungerSystem>(args);
        hunger.ModifyHunger(args.TargetEntity, PotencyPerSecond * -5);
    }
}
