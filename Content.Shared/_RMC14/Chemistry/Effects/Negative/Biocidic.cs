using Content.Shared._RMC14.Botany;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Biocidic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> BluntType = "Blunt";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Deals [color=red]{PotencyPerSecond}[/color] brute damage.\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond * 2}[/color] brute damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 5}[/color] brute damage";
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (plant.Comp.Tray is not { } trayUid || GetTray(args.EntityManager, plant) is not { } tray)
            return;

        var plantTray = args.EntityManager.System<SharedRMCPlantTraySystem>();
        var amount = Potency * (float) args.Quantity;
        if (tray.WeedLevel > 0)
            plantTray.SetWeedLevel((trayUid, tray), MathF.Max(0f, tray.WeedLevel - amount));

        if (tray.PestLevel > 0)
            plantTray.SetPestLevel((trayUid, tray), MathF.Max(0f, tray.PestLevel - amount));
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = potency * 2f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = potency * 5f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
