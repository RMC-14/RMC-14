using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Neuropeutic : RMCChemicalEffect
{
    public override string Abbreviation => "NRP";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        // TODO RMC14 organ brain
        return //$"Heals [color=green]{3 * PotencyPerSecond}[/color] brain damage.\n" +
            $"Overdoses cause [color=red]{PotencyPerSecond}[/color] toxin damage.\n" +
            $"Critical overdoses cause [color=red]{3 * PotencyPerSecond}[/color] brain damage";// and stuns for [color=red]{2 * PotencyPerSecond}[/color] seconds";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 HEAL organ brain
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, PoisonType, potency);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 organ damage brain
        var stun = System<SharedStunSystem>(args);
        stun.TryStun(args.TargetEntity, TimeSpan.FromSeconds(potency.Float() * 2f), true);
    }
}
