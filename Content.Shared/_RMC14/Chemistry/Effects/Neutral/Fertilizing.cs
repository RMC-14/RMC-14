using Content.Shared._RMC14.Botany;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Fertilizing : RMCChemicalEffect
{
    [DataField]
    public float HealthMod = 0.8f;

    [DataField]
    public float YieldMod = 0.3f;

    [DataField]
    public float NutrientMod = 2f;

    [DataField]
    public float LifespanMod;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Acts as a potent fertilizer, feeding plants in a hydroponics tray while improving their health and yield.";
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var amount = Potency * (float) args.Quantity;
        var plantTray = args.EntityManager.System<SharedRMCPlantTraySystem>();
        plantTray.AdjustHealth((plant.Owner, plant.Comp, null), amount * HealthMod);
        AddYieldMod(args.EntityManager, plant, amount * YieldMod);

        if (plant.Comp.Tray is { } trayUid && GetTray(args.EntityManager, plant) is { } tray)
            plantTray.AdjustNutritionLevel((trayUid, tray), amount * NutrientMod);

        if (LifespanMod == 0f)
            return;

        if (args.EntityManager.TryGetComponent<RMCPlantGrowthComponent>(plant.Owner, out var growth))
            plantTray.SetLifespan((plant.Owner, growth), growth.Lifespan + amount * LifespanMod);
    }
}
