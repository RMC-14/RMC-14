using Content.Shared._RMC14.Medical.Vomit;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Emetic : RMCChemicalEffect
{
    public override string Abbreviation => "EME";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Acts on the enteric nervous system to induce emesis, the forceful emptying of the stomach.\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond * 0.5}[/color] toxin damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 0.5}[/color] toxin damage";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var volume = args is { Reagent: not null, Source: not null }
            ? args.Source.GetTotalPrototypeQuantity(args.Reagent.ID)
            : FixedPoint2.Zero;

        if (ProbHundred(volume * potency))
        {
            var rmcVomit = System<RMCVomitSystem>(args);
            rmcVomit.StartVomit(args.TargetEntity);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, PoisonType, 0.5 * potency);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, PoisonType, 0.5 * potency);
    }
}
