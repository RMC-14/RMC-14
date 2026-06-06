using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private void TryStartFightOrFlight(Entity<RMCGiantLizardComponent> ent, EntityUid target)
    {
        if (!TryGetHealthFraction(ent.Owner, ent.Comp, out var healthFraction) ||
            healthFraction > ent.Comp.FightOrFlightHealthFraction)
        {
            return;
        }

        var chance = Math.Clamp(1f - healthFraction, 0f, 1f);
        TryStartRetreat(ent, target, chance);
    }

    private bool TryStartDesperateRetreat(Entity<RMCGiantLizardComponent, TransformComponent> ent, EntityUid target)
    {
        if (!IsLowHealth(ent.Owner, ent.Comp1))
            return false;

        return TryStartRetreat((ent.Owner, ent.Comp1), target, 1f);
    }

    private bool TryStartRetreat(Entity<RMCGiantLizardComponent> ent, EntityUid target, float chance, TimeSpan? duration = null, bool ignoreCooldown = false)
    {
        if (ActorQuery.HasComp(ent.Owner) ||
            ent.Comp.Retreating ||
            ent.Comp.Leaping ||
            ent.Comp.RavageTarget != null ||
            (!ignoreCooldown && ent.Comp.NextRetreatAt > Timing.CurTime) ||
            IsOnFire(ent.Owner) ||
            !ValidLizardTarget(target) ||
            Faction.IsEntityFriendly(ent.Owner, target) ||
            !Random.Prob(chance))
        {
            return false;
        }

        WakeRest(ent);
        StopRoam(ent, false);
        if (ent.Comp.Skirmishing)
            StopSkirmish(ent);

        if (ent.Comp.FoodTarget != null || ent.Comp.EatingFood)
            LoseFoodTarget(ent);

        ent.Comp.Retreating = true;
        ent.Comp.RetreatTarget = target;
        ent.Comp.RetreatUntil = Timing.CurTime + (duration ?? ent.Comp.RetreatDuration);
        ent.Comp.NextRetreatMoveAt = TimeSpan.Zero;
        Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-retreats", ("lizard", ent.Owner), ("target", target)), ent.Owner, PopupType.MediumCaution);

        return true;
    }

    private bool UpdateRetreat(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        if (!ent.Comp1.Retreating)
            return false;

        if (ent.Comp1.RetreatUntil <= Timing.CurTime)
        {
            return StopRetreat((ent.Owner, ent.Comp1));
        }

        if (ent.Comp1.NextRetreatMoveAt > Timing.CurTime)
            return true;

        ent.Comp1.NextRetreatMoveAt = Timing.CurTime + ent.Comp1.RetreatRepathCooldown;

        var speed = ent.Comp1.RetreatTarget == null && IsOnFire(ent.Owner)
            ? ent.Comp1.FirePanicSpeed
            : ent.Comp1.RetreatSpeed;

        if (ent.Comp1.RetreatTarget is { } target &&
            ValidLizardTarget(target) &&
            XformQuery.HasComp(target))
        {
            TryMoveAwayFrom(ent.Owner, Transform.GetMoverCoordinates(target), speed);
        }
        else
        {
            TryMoveRandomly(ent.Owner, speed);
        }

        if (ent.Comp1.RetreatTarget != null)
            TryBreakNearbyObstacle(ent);

        return true;
    }

    private bool StopRetreat(Entity<RMCGiantLizardComponent> ent)
    {
        ent.Comp.Retreating = false;
        ent.Comp.RetreatTarget = null;
        StopMovement(ent.Owner);

        if (TryResistFire(ent))
            return true;

        if (!IsLowHealth(ent.Owner, ent.Comp))
        {
            ent.Comp.RetreatAttempts = 0;
            return false;
        }

        if (ent.Comp.RetreatAttempts >= ent.Comp.RetreatMaxAttempts)
        {
            ent.Comp.RetreatAttempts = 0;
            ent.Comp.NextRetreatAt = Timing.CurTime + ent.Comp.RetreatCooldown;
            return false;
        }

        if (!TryPickRetreatThreat((ent.Owner, ent.Comp), out var threat))
        {
            ent.Comp.RetreatAttempts = 0;
            return false;
        }

        ent.Comp.RetreatAttempts++;
        return TryStartRetreat(ent, threat, 1f, ent.Comp.RetreatReattemptDuration, true);
    }

    private bool TryPickRetreatThreat(Entity<RMCGiantLizardComponent> ent, out EntityUid threat)
    {
        threat = default;
        var coords = Transform.GetMapCoordinates(ent.Owner);
        var bestDistance = float.MaxValue;

        foreach (var candidate in Lookup.GetEntitiesInRange<MobStateComponent>(coords, ent.Comp.RetreatReattemptRange))
        {
            if (candidate.Owner == ent.Owner ||
                !ValidLizardTarget(candidate.Owner) ||
                Faction.IsEntityFriendly(ent.Owner, candidate.Owner))
            {
                continue;
            }

            var candidateCoords = Transform.GetMapCoordinates(candidate.Owner);
            if (candidateCoords.MapId != coords.MapId)
                continue;

            var distance = (candidateCoords.Position - coords.Position).Length();
            if (distance >= bestDistance)
                continue;

            threat = candidate.Owner;
            bestDistance = distance;
        }

        return threat != default;
    }
}
