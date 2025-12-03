using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Neuropathic : RMCChemicalEffect
{
    public override string Abbreviation => "NPT";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        // TODO RMC14 apply pain
        return $"Overdoses cause [color=red]{PotencyPerSecond}[/color] blunt damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 2}[/color] blunt damage.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 apply pain
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 apply pain
        // TODO RMC14 one limb at random
        TryChangeDamage(args, BluntType, potency);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 one limb at random
        TryChangeDamage(args, BluntType, potency * 2);
    }
}
