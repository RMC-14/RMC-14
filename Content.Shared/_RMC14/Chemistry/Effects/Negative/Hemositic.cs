using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Hemositic : RMCChemicalEffect
{
    public override string Abbreviation => "HST";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        // TODO RMC14 cancel if species is synthetic
        // TODO RMC14 disability nervous
        return $"If the user is not hungry, removes [color=red]{PotencyPerSecond * 3}[/color] units of blood and adds [color=red]1[/color] unit of this reagent to the bloodstream. If the user is hungry, it only removes [color=red]{PotencyPerSecond * 0.5}[/color] units of blood and adds no units of itself.\n" +
               $"Overdoses remove [color=red]{PotencyPerSecond * 10}[/color] units of blood and adds [color=red]{PotencyPerSecond * 2}[/color] units of this reagent to the bloodstream.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var bloodstream = System<SharedBloodstreamSystem>(args);
        if (TryComp(args, out HungerComponent? hunger) &&
            hunger.CurrentThreshold >= HungerThreshold.Peckish)
        {
            bloodstream.TryModifyBloodLevel(args.TargetEntity, -potency * 3);

            if (args.Source != null && args.Reagent != null)
                args.Source.AddReagent(args.Reagent.ID, FixedPoint2.New(1));
        }
        else
        {
            bloodstream.TryModifyBloodLevel(args.TargetEntity, -potency * 0.5);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var bloodstream = System<SharedBloodstreamSystem>(args);
        bloodstream.TryModifyBloodLevel(args.TargetEntity, -potency * 10);

        if (args.Source != null && args.Reagent != null)
            args.Source.AddReagent(args.Reagent.ID, potency * 2);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 disability nervous
    }
}
