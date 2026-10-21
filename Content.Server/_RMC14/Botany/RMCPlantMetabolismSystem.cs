using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Botany;

/// <summary>
/// Water/nutrient consumption ticks & soil solution reagent metabolism
/// </summary>
public sealed class RMCPlantMetabolismSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public const float HydroponicsSpeedMultiplier = 1f;
    public const float HydroponicsConsumptionMultiplier = 1.5f;

    /// <summary>
    /// Consumes nutrients/water
    /// </summary>
    public void Tick(EntityUid plant, Entity<RMCPlantTrayComponent> tray)
    {
        if (!TryComp(plant, out RMCPlantMetabolismComponent? metabolism))
            return;

        if (metabolism.NutrientConsumption > 0 && tray.Comp.NutritionLevel > 0 && _random.Prob(0.25f))
        {
            AdjustNutrient(tray, -MathF.Max(0f, metabolism.NutrientConsumption * HydroponicsSpeedMultiplier));
            if (tray.Comp.DrawWarnings)
                tray.Comp.UpdateSpriteAfterUpdate = true;
        }

        if (metabolism.WaterConsumption > 0 && tray.Comp.WaterLevel > 0 && _random.Prob(0.25f))
        {
            AdjustWater(tray, -MathF.Max(0f,
                metabolism.WaterConsumption * HydroponicsConsumptionMultiplier * HydroponicsSpeedMultiplier));
            if (tray.Comp.DrawWarnings)
                tray.Comp.UpdateSpriteAfterUpdate = true;
        }
    }

    public void AdjustNutrient(Entity<RMCPlantTrayComponent> tray, float amount)
    {
        tray.Comp.NutritionLevel += amount;
        DirtyField(tray.Owner, tray.Comp, nameof(RMCPlantTrayComponent.NutritionLevel));
    }

    public void AdjustWater(Entity<RMCPlantTrayComponent> tray, float amount)
    {
        tray.Comp.WaterLevel += amount;
        DirtyField(tray.Owner, tray.Comp, nameof(RMCPlantTrayComponent.WaterLevel));
    }

    public void UpdateReagents(EntityUid trayUid, RMCPlantTrayComponent tray, EntityUid? plant)
    {
        if (!_solutionContainer.ResolveSolution(trayUid, tray.SoilSolutionName, ref tray.SoilSolution, out var solution))
            return;

        if (plant == null || solution.Volume <= 0)
            return;

        var amt = FixedPoint2.New(1);
        foreach (var entry in _solutionContainer.RemoveEachReagent(tray.SoilSolution.Value, amt))
        {
            if (_rmcReagent.TryIndex(entry.Reagent.Prototype, out var reagentProto))
                reagentProto.ReactionPlant(plant.Value, entry, solution);
        }
    }
}
