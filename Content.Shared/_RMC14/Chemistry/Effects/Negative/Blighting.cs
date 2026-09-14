using Content.Shared._RMC14.Botany;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
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
        return "Infests a hydroponics tray with pests, starves its plants of nutrients, and slowly mutates their line into a worse producer.";
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

        var immutable = args.EntityManager.TryGetComponent<RMCPlantMutationComponent>(plant.Owner, out var mutation) && mutation.Immutable;
        if (immutable || growth.Production <= MinProduction)
            return;

        if (!IoCManager.Resolve<IRobustRandom>().Prob(amount / DivergeVolume))
            return;

        plantTray.SetProduction((plant.Owner, growth), MathF.Max(0f, growth.Production - 1f));
    }
}
