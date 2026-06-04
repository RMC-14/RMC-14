using Content.Shared._RMC14.Mobs.Animals;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private bool TryAiFeed(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        if (ent.Comp1.EatingFood)
            return UpdateEatingFood((ent.Owner, ent.Comp1));

        EntityUid foodTarget;
        if (ent.Comp1.FoodTarget is { } existingFoodTarget)
        {
            foodTarget = existingFoodTarget;
        }
        else
        {
            if (ent.Comp1.NextFoodSearchAt > Timing.CurTime)
                return false;

            var pickedFood = PickFoodTarget(ent);
            if (pickedFood == null)
                return false;

            foodTarget = pickedFood.Value;
            ent.Comp1.FoodTarget = foodTarget;
        }

        if (!IsValidFoodTarget(foodTarget))
        {
            LoseFoodTarget((ent.Owner, ent.Comp1));
            return true;
        }

        if (TryHandleFoodHolder((ent.Owner, ent.Comp1), foodTarget))
            return true;

        var lizardCoords = Transform.GetMoverCoordinates(ent.Owner);
        var targetCoords = Transform.GetMoverCoordinates(foodTarget);
        if (!lizardCoords.TryDistance(EntityManager, targetCoords, out var targetDistance) ||
            targetDistance > ent.Comp1.FoodTargetKeepRange)
        {
            LoseFoodTarget((ent.Owner, ent.Comp1));
            return true;
        }

        if (targetDistance > ent.Comp1.AiFeedRange)
        {
            WakeRest((ent.Owner, ent.Comp1));
            TryMoveTowards(ent.Owner, targetCoords, ent.Comp1.ForageSpeed);
            return true;
        }

        StartEatingFood((ent.Owner, ent.Comp1), foodTarget);
        return true;
    }
}
