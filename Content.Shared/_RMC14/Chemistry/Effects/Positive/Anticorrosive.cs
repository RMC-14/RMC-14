using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Anticorrosive : RMCChemicalEffect
{
    public override string Abbreviation => "ACR";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var healing = PotencyPerSecond;
        if (Potency > 2)
            healing += PotencyPerSecond * 0.5f;

        return $"Heals [color=green]{healing}[/color] burn damage.\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond}[/color] brute and [color=red]{PotencyPerSecond}[/color] toxin damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 5}[/color] brute and [color=red]{PotencyPerSecond * 5}[/color] toxin damage";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryHealDamageGroup(args, BurnGroup, potency);
        if (Potency > 2)
        {
            TryHealDamageGroup(args, BurnGroup, potency * 0.5f);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, BluntType, potency);
        TryChangeDamage(args, PoisonType, potency);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, BluntType, potency * 5);
        TryChangeDamage(args, PoisonType, potency * 5);
    }
}
