using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Carcinogenic : RMCChemicalEffect
{
    public override string Abbreviation => "CRG";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Deals [color=red]{PotencyPerSecond * 0.5}[/color] genetic damage.\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond * 2}[/color] genetic damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 2}[/color] brute damage.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, GeneticType, potency * 0.5);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, GeneticType, potency * 2);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 one limb at random
        TryChangeDamage(args, BluntType, potency * 2);
    }
}
