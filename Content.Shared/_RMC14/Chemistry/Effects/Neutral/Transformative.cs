using Content.Shared._RMC14.Damage;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Transformative : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";

    [DataField]
    public float HealAmount = 0.75f;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Mends damaged tissue, producing a small amount of toxin as a byproduct.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.TryGetComponent<DamageableComponent>(args.TargetEntity, out var damageableComp))
            return;

        var prototypes = IoCManager.Resolve<IPrototypeManager>();
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var heal = HealAmount * (float) potency * 2f;
        var damage = new DamageSpecifier();
        var byproduct = FixedPoint2.Zero;

        if (prototypes.TryIndex(BruteGroup, out var bruteGroup) &&
            damageableComp.Damage.TryGetDamageInGroup(bruteGroup, out var bruteDamage) &&
            bruteDamage > FixedPoint2.Zero)
        {
            damage = rmcDamageable.DistributeHealingCached((args.TargetEntity, damageableComp), BruteGroup, heal, damage);
            byproduct += heal * 0.1f;
        }

        if (prototypes.TryIndex(BurnGroup, out var burnGroup) &&
            damageableComp.Damage.TryGetDamageInGroup(burnGroup, out var burnDamage) &&
            burnDamage > FixedPoint2.Zero)
        {
            damage = rmcDamageable.DistributeHealingCached((args.TargetEntity, damageableComp), BurnGroup, heal, damage);
            byproduct += heal * 0.1f;
        }

        if (byproduct > FixedPoint2.Zero)
            damage.DamageDict[PoisonType] = byproduct;

        if (damage.DamageDict.Count == 0)
            return;

        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = HealAmount * (float) potency * 0.5f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = HealAmount * (float) potency * 2f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
