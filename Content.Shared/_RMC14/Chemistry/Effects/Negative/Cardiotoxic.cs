using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Cardiotoxic : RMCChemicalEffect
{
    public override string Abbreviation => "CDT";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        // TODO RMC14 organ heart damage
        return $"Overdoses cause [color=red]{PotencyPerSecond * 2}[/color] asphyxiation damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 5}[/color] asphyxiation damage.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!IsHumanoid(args))
            return;

        // TODO RMC14 organ heart damage
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, AsphyxiationType, potency * 2);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, AsphyxiationType, potency * 5);
    }
}
