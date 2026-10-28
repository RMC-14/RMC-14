using Content.Shared._RMC14.Botany;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Blighting : RMCChemicalEffect
{
    [DataField]
    public float PestMod = 2f;

    [DataField]
    public float NutrientDrain = 2f;

    [DataField]
    public float MinProduction = 1f;

    [DataField]
    public float DivergeVolume = 30f;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Infests a hydroponics tray with pests, starves its plants of nutrients, and slowly shortens the time between their harvests.";
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var amount = Potency * (float) args.Quantity;
        var plantTray = args.EntityManager.System<SharedRMCPlantTraySystem>();
        if (plant.Comp.Tray is { } trayUid && GetTray(args.EntityManager, plant) is { } tray)
        {
            plantTray.AdjustPestLevel((trayUid, tray), amount * PestMod);
            plantTray.AdjustNutritionLevel((trayUid, tray), -amount * NutrientDrain);
        }

        if (!args.EntityManager.TryGetComponent<RMCPlantGrowthComponent>(plant.Owner, out var growth))
            return;

        var immutable = args.EntityManager.TryGetComponent<RMCPlantMutationComponent>(plant.Owner, out var mutation) && mutation.MutationTier == RMCPlantMutationTier.Immutable;
        if (immutable || growth.Production <= MinProduction)
            return;

        var counter = plant.Comp.BlightProductionCounter + amount;
        if (counter < DivergeVolume)
        {
            plantTray.SetBlightProductionCounter((plant.Owner, plant.Comp), counter);
            return;
        }

        plantTray.SetBlightProductionCounter((plant.Owner, plant.Comp), 0f);
        plantTray.SetProduction((plant.Owner, growth), MathF.Max(0f, growth.Production - 1f));

        if (IoCManager.Resolve<IRobustRandom>().Prob(0.5f))
        {
            var popup = args.EntityManager.System<SharedPopupSystem>();
            popup.PopupEntity(Loc.GetString("plant-blighting-sway", ("name", Loc.GetString(plant.Comp.DisplayName))), args.TargetEntity);
        }
    }
}
