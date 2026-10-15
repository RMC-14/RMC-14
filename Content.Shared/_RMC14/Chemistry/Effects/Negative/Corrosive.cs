using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Corrosive : RMCChemicalEffect
{
    public override string Abbreviation => "CRS";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Deals [color=red]{PotencyPerSecond}[/color] burn damage.\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond * 2}[/color] burn damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 5}[/color] burn damage.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 one limb at random
        TryChangeDamage(args, CausticType, potency);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 one limb at random
        TryChangeDamage(args, CausticType, potency * 2);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 one limb at random
        TryChangeDamage(args, CausticType, potency * 5);
    }
}
