using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Synth;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared._RMC14.Xenonids;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Toxic : RMCChemicalEffect
{
    public override bool ReactsOnTouch => true;

    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Deals [color=red]{PotencyPerSecond}[/color] toxin damage.\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond * 2}[/color] toxin damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 5}[/color] toxin damage";
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var amount = Potency * (float) args.Quantity;
        var plantTray = args.EntityManager.System<SharedRMCPlantTraySystem>();
        plantTray.AdjustHealth((plant.Owner, plant.Comp, null), -1.5f * amount);

        if (plant.Comp.Tray is { } trayUid && GetTray(args.EntityManager, plant) is { } tray)
            plantTray.AdjustToxins((trayUid, tray), amount);
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (IsReaction(args))
        {
            HandleReaction(damageable, args);
            return;
        }

        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(ToxinGroup, potency);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    private void HandleReaction(DamageableSystem damageable, EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        var isHuman = entities.HasComponent<HumanoidAppearanceComponent>(target) && !entities.HasComponent<SynthComponent>(target);
        var isXeno = entities.HasComponent<XenoComponent>(target);
        if (!isHuman && !isXeno)
            return;

        if (isHuman && entities.System<InventorySystem>().TryGetSlotEntity(target, "mask", out _))
            return;

        var rmcDamageable = entities.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(ToxinGroup, ActualPotency);
        damageable.TryChangeDamage(target, damage, true, interruptsDoAfters: false);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(ToxinGroup, potency * 2f);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(ToxinGroup, potency * 5f);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
