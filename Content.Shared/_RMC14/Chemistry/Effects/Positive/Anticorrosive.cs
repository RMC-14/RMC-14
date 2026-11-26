using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Medical.Wounds;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Anticorrosive : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";

    private static readonly ProtoId<DamageTypePrototype> BluntType = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var healing = PotencyPerSecond;
        if (ActualPotency > 2)
            healing += PotencyPerSecond * 0.5f;

        return $"Heals [color=green]{healing}[/color] burn damage.\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond}[/color] brute and [color=red]{PotencyPerSecond}[/color] toxin damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 5}[/color] brute and [color=red]{PotencyPerSecond * 5}[/color] toxin damage";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var healing = rmcDamageable.DistributeHealingCached(args.TargetEntity, BurnGroup, potency);

        if (args.EntityManager.TryGetComponent(args.TargetEntity, out DamageableComponent? targetDamageable) &&
            args.EntityManager.TrySystem(out SharedWoundsSystem? wounds))
        {
            wounds.ClampEscharBurnHealing(args.TargetEntity, targetDamageable, healing);
        }

        damageable.TryChangeDamage(args.TargetEntity, healing, true, interruptsDoAfters: false);
        if (ActualPotency > 2)
        {
            healing = rmcDamageable.DistributeHealingCached(args.TargetEntity, BurnGroup, potency * 0.5f);
            if (args.EntityManager.TryGetComponent(args.TargetEntity, out DamageableComponent? boostedDamageable) &&
                args.EntityManager.TrySystem(out SharedWoundsSystem? boostedWounds))
            {
                boostedWounds.ClampEscharBurnHealing(args.TargetEntity, boostedDamageable, healing);
            }
            damageable.TryChangeDamage(args.TargetEntity, healing, true, interruptsDoAfters: false);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = potency;
        damage.DamageDict[PoisonType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = potency * 5;
        damage.DamageDict[PoisonType] = potency * 5;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
