using Content.Shared._RMC14.Damage;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Anticarcinogenic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> GeneticGroup = "Genetic";
    private static readonly ProtoId<DamageTypePrototype> BluntType = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Heals [color=green]{PotencyPerSecond}[/color] genetic damage.\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond}[/color] toxin damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 2}[/color] brute damage";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var healing = rmcDamageable.DistributeHealingCached(args.TargetEntity, GeneticGroup, potency);
        damageable.TryChangeDamage(args.TargetEntity, healing, true, interruptsDoAfters: false);
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
        damage.DamageDict[BluntType] = potency * 2f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
