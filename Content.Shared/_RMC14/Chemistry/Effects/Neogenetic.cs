using Content.Shared._RMC14.Damage;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Chemistry.Effects;

public sealed partial class Neogenetic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageTypePrototype> HeatType = "Heat";
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return null;
        throw new NotImplementedException();
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var cmDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var healing = cmDamageable.DistributeHealing(args.TargetEntity, BruteGroup, potency);

        damageable.TryChangeDamage(args.TargetEntity, healing, true, interruptsDoAfters: false);
        if (ActualPotency > 2)
        {
            healing = cmDamageable.DistributeHealing(args.TargetEntity, BruteGroup, potency * 0.5f);
            damageable.TryChangeDamage(args.TargetEntity, healing, true, interruptsDoAfters: false);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[HeatType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[HeatType] = potency * 5;
        damage.DamageDict[PoisonType] = potency * 2;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
