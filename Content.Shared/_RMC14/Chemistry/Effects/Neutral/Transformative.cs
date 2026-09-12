using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Transformative : RMCChemicalEffect
{
    public override string Abbreviation => "TRF";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var healing = 0.75 * PotencyPerSecond * 2;
        return $"Heals [color=green]{healing}[/color] brute and burn damage.\n" +
               $"Deals [color=red]{healing * 0.1}[/color] toxin damage when brute and/or burn heals.\n" +
               $"Overdoses cause [color=red]{0.75 * PotencyPerSecond * 0.5}[/color] toxin damage.\n" +
               $"Critical overdoses cause [color=red]{healing}[/color] toxin damage";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var healing = 0.75 * potency * 2;
        if (TryComp<DamageableComponent>(args, out var dmgComp))
        {
            if (dmgComp.DamagePerGroup.GetValueOrDefault(BruteGroup) > 0)
            {
                TryHealDamageGroup(args, BruteGroup, healing);
                TryChangeDamage(args, PoisonType, 0.1 * healing);
            }

            if (dmgComp.DamagePerGroup.GetValueOrDefault(BurnGroup) > 0)
            {
                TryHealDamageGroup(args, BurnGroup, healing);
                TryChangeDamage(args, PoisonType, 0.1 * healing);
            }
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, PoisonType, 0.75 * potency * 0.5);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, PoisonType, 0.75 * potency * 2);
    }
}
