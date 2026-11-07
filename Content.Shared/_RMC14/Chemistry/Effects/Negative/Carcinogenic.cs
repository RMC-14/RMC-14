using Content.Shared._RMC14.Botany;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Carcinogenic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> GeneticType = "Cellular";
    private static readonly ProtoId<DamageTypePrototype> BluntType = "Blunt";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Deals [color=red]{PotencyPerSecond * 0.5}[/color] genetic damage.\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond * 2}[/color] genetic damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 2}[/color] brute damage";
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var amount = Potency * (float) args.Quantity;
        var plantTray = args.EntityManager.System<SharedRMCPlantTraySystem>();
        if (plant.Comp.Tray is { } trayUid && GetTray(args.EntityManager, plant) is { } tray)
            plantTray.AdjustToxins((trayUid, tray), 1.5f * amount);

        plantTray.AdjustMutationLevel((plant.Owner, plant.Comp), 10f * amount + plant.Comp.MutationMod);
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[GeneticType] = potency * 0.5f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[GeneticType] = potency * 2f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = potency * 2f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
