using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Stun;
using Content.Shared.Damage;
using Content.Shared.Drunk;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Ketogenic : RMCChemicalEffect
{
    public override string Abbreviation => "KTG";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Removes [color=red]{PotencyPerSecond * 5}[/color] nutrients, causing hunger over time.\n" +
               $"Metabolizes [color=green]{PotencyPerSecond}u[/color] of alcohol per second.\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond * 5}[/color] nutrition loss, [color=red]{PotencyPerSecond}[/color] toxin damage, and a [color=red]{Potency * 2.5}%[/color] chance of vomiting.\n" +
               $"Critical overdoses will knock you unconscious for [color=red]40[/color] seconds";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var hungerSys = System<HungerSystem>(args);
        hungerSys.ModifyHunger(args.TargetEntity, PotencyPerSecond * -5);
        // TODO RMC14 M.overeatduration = 0

        var bloodstream = System<SharedRMCBloodstreamSystem>(args);
        var alcoholRemoved = bloodstream.RemoveBloodstreamAlcohols(args.TargetEntity, potency);
        if (!alcoholRemoved)
            return;

        var drunkSys = System<SharedDrunkSystem>(args);
        drunkSys.TryApplyDrunkenness(args.TargetEntity, PotencyPerSecond * 5);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var hungerSys = System<HungerSystem>(args);
        hungerSys.ModifyHunger(args.TargetEntity, PotencyPerSecond * -5);

        TryChangeDamage(args, PoisonType, potency);

        if (ProbHundred(2.5 * Potency))
        {
            var rmcVomitSys = System<RMCVomitSystem>(args);
            rmcVomitSys.StartVomit(args.TargetEntity);
        }
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var knockOut = System<RMCSizeStunSystem>(args);
        knockOut.TryKnockOut(args.TargetEntity, TimeSpan.FromSeconds(40), true);
    }
}
