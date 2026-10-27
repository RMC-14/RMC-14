using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.Popups;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private void StartEatingFood(Entity<RMCGiantLizardComponent> ent, EntityUid food)
    {
        WakeRest(ent);
        StopMovement(ent.Owner);
        ent.Comp.EatingFood = true;
        ent.Comp.FoodBitesLeft = Random.Next(ent.Comp.FoodBitesMin, ent.Comp.FoodBitesMax + 1);
        ent.Comp.NextFoodBiteAt = Timing.CurTime + RandomTime(ent.Comp.FoodBiteDelayMin, ent.Comp.FoodBiteDelayMax);
        Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-starts-gnawing", ("lizard", ent.Owner), ("food", food)), ent.Owner);
    }

    private bool UpdateEatingFood(Entity<RMCGiantLizardComponent> ent)
    {
        if (ent.Comp.FoodTarget is not { } food || !IsValidFoodTarget(food))
        {
            LoseFoodTarget(ent);
            return true;
        }

        if (TryHandleFoodHolder(ent, food))
            return true;

        var lizardCoords = Transform.GetMoverCoordinates(ent.Owner);
        var targetCoords = Transform.GetMoverCoordinates(food);
        if (!lizardCoords.TryDistance(EntityManager, targetCoords, out var distance) ||
            distance > ent.Comp.AiFeedRange ||
            !MobState.IsAlive(ent.Owner))
        {
            LoseFoodTarget(ent);
            return true;
        }

        StopMovement(ent.Owner);
        if (ent.Comp.NextFoodBiteAt > Timing.CurTime)
            return true;

        ent.Comp.FoodBitesLeft--;
        _audio.PlayPvs(ent.Comp.EatingSound, ent.Owner);

        if (ent.Comp.FoodBitesLeft > 0)
        {
            ent.Comp.NextFoodBiteAt = Timing.CurTime + RandomTime(ent.Comp.FoodBiteDelayMin, ent.Comp.FoodBiteDelayMax);
            return true;
        }

        FinishEatingFood(ent, food);
        return true;
    }

    private void FinishEatingFood(Entity<RMCGiantLizardComponent> ent, EntityUid food)
    {
        if (_lastFoodHolder.TryGetValue(food, out var feeder) &&
            ValidLivingMob(feeder) &&
            Transform.GetMoverCoordinates(ent.Owner).TryDistance(EntityManager, Transform.GetMoverCoordinates(feeder), out var feederDistance) &&
            feederDistance <= ent.Comp.AiFeedTameRange &&
            !Faction.IsEntityFriendly(ent.Owner, feeder))
        {
            if (TryTameToFeeder(ent.Owner, feeder, ent.Comp))
                Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-stares-curiously", ("lizard", ent.Owner), ("user", feeder)), ent.Owner);
        }

        HealFraction(ent.Owner, ent.Comp.AiFeedHealFraction);
        QueueDel(food);
        ent.Comp.FoodTarget = null;
        ent.Comp.EatingFood = false;
        ent.Comp.FoodBitesLeft = 0;
        ent.Comp.NextFoodSearchAt = Timing.CurTime + ent.Comp.FoodEatenCooldown;
        UpdateLizardVisuals(ent);
    }

    private void LoseFoodTarget(Entity<RMCGiantLizardComponent> ent)
    {
        StopMovement(ent.Owner);
        ent.Comp.FoodTarget = null;
        ent.Comp.EatingFood = false;
        ent.Comp.FoodBitesLeft = 0;
        ent.Comp.NextFoodSearchAt = Timing.CurTime + ent.Comp.FoodLostCooldown;
    }
}
