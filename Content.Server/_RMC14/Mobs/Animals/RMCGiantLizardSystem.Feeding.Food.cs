using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Robust.Shared.Containers;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private bool TryHandleFoodHolder(Entity<RMCGiantLizardComponent> ent, EntityUid food)
    {
        if (!TryGetFoodHolder(food, out var holder))
            return false;

        LoseFoodTarget(ent);
        PlayGrowl(ent);

        if (!ValidLivingMob(holder) ||
            !Transform.GetMoverCoordinates(ent.Owner).TryDistance(EntityManager, Transform.GetMoverCoordinates(holder), out var distance))
        {
            return true;
        }

        if (distance <= ent.Comp.FoodTheftRetaliateRange && !Faction.IsEntityFriendly(ent.Owner, holder))
        {
            TryAggro(ent.Owner, holder, ent.Comp);
            AlertPack(ent.Owner, holder, ent.Comp);
            Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-food-stolen", ("lizard", ent.Owner), ("user", holder)), ent.Owner, holder, PopupType.MediumCaution);
            return true;
        }

        Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-growls-at", ("lizard", ent.Owner), ("user", holder)), ent.Owner, holder, PopupType.MediumCaution);
        return true;
    }

    private bool TryGetFoodHolder(EntityUid food, out EntityUid holder)
    {
        holder = default;
        if (!Container.TryGetContainingContainer((food, null, null), out var container))
            return false;

        holder = container.Owner;
        return MobQuery.HasComp(holder);
    }

    private bool IsValidFoodTarget(EntityUid food)
    {
        return !TerminatingOrDeleted(food) && IsAcceptedLizardFood(food);
    }

    private bool IsAcceptedLizardFood(EntityUid food)
    {
        if (!HasComp<FoodComponent>(food))
            return false;

        if (Tags.HasAnyTag(food, "Meat"))
            return true;

        var proto = MetaData(food).EntityPrototype?.ID;
        if (proto == null)
            return false;

        return proto.Contains("Meat", StringComparison.OrdinalIgnoreCase) ||
               proto.Contains("MRE", StringComparison.OrdinalIgnoreCase) ||
               proto.Contains("PreparedMeal", StringComparison.OrdinalIgnoreCase) ||
               proto.Contains("Protein", StringComparison.OrdinalIgnoreCase) ||
               proto.Contains("ResinFruit", StringComparison.OrdinalIgnoreCase);
    }
}
