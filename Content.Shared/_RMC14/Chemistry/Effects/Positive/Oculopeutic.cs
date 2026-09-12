using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Oculopeutic : RMCChemicalEffect
{
    public override string Abbreviation => "OCP";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return //$"Heals [color=green]{PotencyPerSecond}[/color] eye damage and restores [color=green]{PotencyPerSecond * 3}[/color] to blindness and blurry vision.\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond}[/color] toxin damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond}[/color] brute, [color=red]{PotencyPerSecond}[/color] burn, and [color=red]{PotencyPerSecond * 3}[/color] toxin damage"; // and brain damage
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!IsHumanoid(args))
            return;

        // TODO RMC14 organ eye healing
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
        // TODO RM14 organ brain damage
    }
}
