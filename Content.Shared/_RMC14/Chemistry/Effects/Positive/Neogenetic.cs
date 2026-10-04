using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Neogenetic : RMCChemicalEffect
{
    public override string Abbreviation => "NGN";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var healing = PotencyPerSecond;
        if (Potency > 2)
            healing += PotencyPerSecond * 0.5f;

        return $"Heals [color=green]{healing}[/color] brute damage.\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond}[/color] burn damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 5}[/color] burn and [color=red]{PotencyPerSecond * 2}[/color] toxin damage";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryHealDamageGroup(args, BruteGroup, potency);
        if (Potency > 2)
        {
            TryHealDamageGroup(args, BruteGroup, potency * 0.5f);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, HeatType, potency);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, HeatType, potency * 5);
        TryChangeDamage(args, PoisonType, potency * 2);
    }
}
