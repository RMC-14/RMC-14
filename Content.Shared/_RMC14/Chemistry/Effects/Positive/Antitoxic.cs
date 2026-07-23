using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Stun;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Antitoxic : RMCChemicalEffect
{
    public override string Abbreviation => "ATX";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var healing = PotencyPerSecond * 2;
        return $"Heals [color=green]{healing}[/color] toxin damage and removes [color=green]0.125[/color] units of toxic chemicals from the bloodstream.\n" +
               //$"Overdoses cause [color=red]{PotencyPerSecond}[/color] damage to the eyes.\n" +
               $"Critical overdoses cause a [color=red]5%[/color] chance to inflict [color=red]10[/color] seconds of unconsciousness";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryHealDamageGroup(args, ToxinGroup, potency * 2f);

        // TODO RMC14 remove genetic heal once other meds are in for genetic damage
        TryHealDamageGroup(args, GeneticGroup, potency * 2f);

        var rmcBloodstream = System<SharedRMCBloodstreamSystem>(args);
        rmcBloodstream.RemoveBloodstreamToxins(args.TargetEntity, 0.125f);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 eye damage
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 Drowsiness. if drowsiness > 10 5% change to paralyze(knockout) for 10 seconds.
        var knockOut = System<RMCSizeStunSystem>(args);
        if (ProbHundred(5))
            knockOut.TryKnockOut(args.TargetEntity, TimeSpan.FromSeconds(10), true);
    }
}
