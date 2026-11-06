using Content.Shared._RMC14.Body;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Oxygenating : RMCChemicalEffect
{
    private static readonly ProtoId<ReagentPrototype> Lexorin = "RMCLexorin";

    public override string Abbreviation => "OXG";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var healing = Potency >= 3
            ? $"Heals [color=green]all[/color] airloss damage and removes [color=green]{PotencyPerSecond}[/color] Lexorin from the bloodstream."
            : $"Heals [color=green]{PotencyPerSecond}[/color] airloss damage and removes [color=green]{PotencyPerSecond}[/color] Lexorin from the bloodstream.";

        return $"{healing}\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond * 0.5}[/color] toxin damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond}[/color] brute and [color=red]{PotencyPerSecond * 2}[/color] toxin damage";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var amount = Potency >= 3 ? 99999 : potency;
        TryHealDamageGroup(args, AirlossGroup, amount);

        var rmcBloodstream = System<SharedRMCBloodstreamSystem>(args);
        rmcBloodstream.RemoveBloodstreamChemical(args.TargetEntity, Lexorin, potency);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, PoisonType, potency * 0.5f);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, BluntType, potency);
        TryChangeDamage(args, PoisonType, potency * 2f);
    }
}
