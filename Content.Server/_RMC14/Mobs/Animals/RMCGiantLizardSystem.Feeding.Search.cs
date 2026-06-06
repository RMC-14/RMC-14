using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.Nutrition.Components;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private EntityUid? PickFoodTarget(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        var mapCoords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        EntityUid? bestFood = null;
        var bestDistance = float.MaxValue;

        foreach (var food in Lookup.GetEntitiesInRange<FoodComponent>(mapCoords, ent.Comp1.FoodSearchRange))
        {
            if (!IsAcceptedLizardFood(food.Owner) || !XformQuery.TryGetComponent(food.Owner, out var foodXform))
                continue;

            var foodCoords = Transform.GetMapCoordinates((food.Owner, foodXform));
            var distance = (foodCoords.Position - mapCoords.Position).Length();
            if (distance > bestDistance)
                continue;

            bestFood = food.Owner;
            bestDistance = distance;
        }

        return bestFood;
    }
}
