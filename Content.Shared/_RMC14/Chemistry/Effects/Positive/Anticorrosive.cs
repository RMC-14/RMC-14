using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.Damage;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Anticorrosive : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var healing = PotencyPerSecond;
        if (ActualPotency > 2)
            healing += PotencyPerSecond * 0.5f;

        return $"Heals [color=green]{healing}[/color] burn damage.\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond}[/color] brute and [color=red]{PotencyPerSecond}[/color] toxin damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 5}[/color] brute and [color=red]{PotencyPerSecond * 5}[/color] toxin damage";
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (GetTray(args.EntityManager, plant) is { Toxins: > 0 })
            args.EntityManager.System<SharedRMCPlantTraySystem>().AdjustHealth((plant.Owner, plant.Comp, null), 0.75f * Potency);
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var healing = rmcDamageable.DistributeHealingCached(args.TargetEntity, BurnGroup, potency);

        damageable.TryChangeDamage(args.TargetEntity, healing, true, interruptsDoAfters: false);
        if (ActualPotency > 2)
        {
            healing = rmcDamageable.DistributeHealingCached(args.TargetEntity, BurnGroup, potency * 0.5f);
            damageable.TryChangeDamage(args.TargetEntity, healing, true, interruptsDoAfters: false);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(BruteGroup, potency);
        damage = rmcDamageable.DistributeFreshDamage(ToxinGroup, potency, damage);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(BruteGroup, potency * 5);
        damage = rmcDamageable.DistributeFreshDamage(ToxinGroup, potency * 5, damage);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
