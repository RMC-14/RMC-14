using Content.Shared._RMC14.Body;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Antihallucinogenic : RMCChemicalEffect
{
    private static readonly EntProtoId<StatusEffectComponent> SeeingRainbows = "StatusEffectSeeingRainbow";

    private static readonly ProtoId<ReagentPrototype> MindbreakerToxin = "RMCMindbreakerToxin";
    private static readonly ProtoId<ReagentPrototype> SpaceDrugs = "RMCSpaceDrugs";

    public override string Abbreviation => "AHL";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Removes [color=green]2.5[/color] units of Mindbreaker Toxin and Space Drugs from the bloodstream. It also stabilizes perceptive abnormalities such as hallucinations\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond}[/color] toxin damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond}[/color] brute, [color=red]{PotencyPerSecond}[/color] burn, and [color=red]{PotencyPerSecond * 3}[/color] toxin damage";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcBloodstream = System<SharedRMCBloodstreamSystem>(args);
        rmcBloodstream.RemoveBloodstreamChemical(args.TargetEntity, MindbreakerToxin, 2.5f);
        rmcBloodstream.RemoveBloodstreamChemical(args.TargetEntity, SpaceDrugs, 2.5f);

        var status = System<SharedStatusEffectsSystem>(args);
        status.TryAddTime(args.TargetEntity, SeeingRainbows, TimeSpan.FromSeconds(PotencyPerSecond * -10)); // SeeingRainbows is M.druggy in CM13
        // TODO RMC14 Hallucination
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, PoisonType, potency);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, BluntType, potency);
        TryChangeDamage(args, HeatType, potency);
        TryChangeDamage(args, PoisonType, potency * 3);
    }
}
