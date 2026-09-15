using Content.Shared._RMC14.Botany;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Botany;

/// <summary>
/// Aging, maturation stage math, lifespan death, & production/harvest
/// </summary>
public sealed class RMCPlantGrowthSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCPlantTraySystem _plantTray = default!;

    public const float HydroponicsSpeedMultiplier = 1f;

    public int GetCurrentGrowthStage(Entity<RMCPlantComponent, RMCPlantGrowthComponent> plant)
    {
        var result = Math.Max(1, (int)(plant.Comp1.Age * plant.Comp2.GrowthStages / plant.Comp2.Maturation));
        return result;
    }

    /// <summary>
    /// Advances age. Runs early in the tick, before the nutrient/water/tolerance/toxin/pest/weed formuls
    /// </summary>
    public void TickAging(Entity<RMCPlantComponent> plant, RMCPlantTrayComponent tray)
    {
        var comp = plant.Comp;
        if (comp.SkipAging > 0)
            _plantTray.AdjustSkipAging((plant.Owner, comp), -1);
        else
        {
            if (tray.NutritionLevel > 0 && tray.WaterLevel > 0 && _random.Prob(0.3f))
                _plantTray.AdjustAge((plant.Owner, comp), (int)(1 * HydroponicsSpeedMultiplier));

            comp.UpdateSpriteAfterUpdate = true;
        }
    }

    /// <summary>
    /// Applies lifespan death and toggles the harvest-ready flag. Runs late in the tick, after the
    /// nutrient/water/tolerance/toxin/pest/weed formulas
    /// </summary>
    public bool TickLifespanAndProduction(Entity<RMCPlantComponent, RMCPlantGrowthComponent> plant)
    {
        var (_, comp, growth) = plant;

        if (comp.Age > growth.Lifespan)
        {
            _plantTray.AdjustHealth((plant.Owner, comp, growth), -(_random.Next(3, 6) * HydroponicsSpeedMultiplier));
            comp.UpdateSpriteAfterUpdate = true;
        }
        else if (comp.Age < 0)
        {
            return true;
        }

        if (TryComp(plant.Owner, out RMCPlantHarvestComponent? harvest) && harvest.ProductPrototypes.Count > 0)
        {
            if (comp.Age > growth.Production && comp.Age - growth.LastProduce > growth.Production && !comp.Harvest)
            {
                _plantTray.SetHarvestReady((plant.Owner, comp), true);
                _plantTray.SetLastProduce((plant.Owner, growth), comp.Age);
            }
        }

        return false;
    }

    /// <summary>
    /// Mutation  growth rate nudge: speeds up/slows down maturation or shifts the next production
    /// </summary>
    public void AffectGrowth(Entity<RMCPlantComponent, RMCPlantGrowthComponent> plant, int amount)
    {
        var (_, comp, growth) = plant;

        if (amount > 0)
        {
            if (comp.Age < growth.Maturation)
                _plantTray.AdjustAge((plant.Owner, comp), amount);
            else if (!comp.Harvest && growth.Production <= 0f)
                _plantTray.SetLastProduce((plant.Owner, growth), growth.LastProduce - amount);
        }
        else
        {
            if (comp.Age < growth.Maturation)
                _plantTray.AdjustSkipAging((plant.Owner, comp), 1);
            else if (!comp.Harvest && growth.Production <= 0f)
                _plantTray.SetLastProduce((plant.Owner, growth), growth.LastProduce + amount);
        }
    }

    /// <summary>
    /// Consumes MetabolismAdjust once its delayed/accelerated cycle has fired. Called once per tray growth cycle.
    /// </summary>
    public void ResetMetabolismAdjust(Entity<RMCPlantComponent> plant)
    {
        _plantTray.SetMetabolismAdjust((plant.Owner, plant.Comp), 0f);
    }

    public void CheckLevelSanity(Entity<RMCPlantComponent, RMCPlantGrowthComponent> plant)
    {
        var (_, comp, growth) = plant;
        if (comp.Health > growth.Endurance || comp.Health < 0f)
            _plantTray.AdjustHealth((plant.Owner, comp, growth), 0f);
        _plantTray.SetMutationLevel((plant.Owner, comp), MathHelper.Clamp(comp.MutationLevel, 0f, 100f));
    }
}
