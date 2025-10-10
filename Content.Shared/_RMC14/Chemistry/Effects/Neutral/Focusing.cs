using Content.Shared._RMC14.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Focusing : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Removes [color=green]{PotencyPerSecond}[/color] units of alcoholic substances from the bloodstream.\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond}[/color] toxin damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 3}[/color] toxin damage";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var bloodstream = args.EntityManager.System<SharedRMCBloodstreamSystem>();
        bloodstream.RemoveBloodstreamAlcohols(args.TargetEntity, potency);
        // remove drunkness and a ton of CC effects
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency * 3;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
