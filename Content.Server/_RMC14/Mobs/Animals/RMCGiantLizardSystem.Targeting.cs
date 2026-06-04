using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private EntityUid? PickLizardTarget(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        var mapCoords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        EntityUid? best = null;
        var bestDistance = float.MaxValue;

        foreach (var target in Faction.GetNearbyHostiles((ent.Owner, null, null), ent.Comp1.TargetSearchRange))
        {
            if (!ValidLizardTarget(target) || !XformQuery.TryGetComponent(target, out var targetXform))
                continue;

            var targetMap = Transform.GetMapCoordinates((target, targetXform));
            if (targetMap.MapId != mapCoords.MapId)
                continue;

            var distance = (targetMap.Position - mapCoords.Position).Length();
            if (distance > bestDistance)
                continue;

            best = target;
            bestDistance = distance;
        }

        return best;
    }

    private bool WarnOrAggroCloseThreat(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        var mapCoords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        EntityUid? warning = null;
        var warningDistance = float.MaxValue;

        foreach (var target in Lookup.GetEntitiesInRange<MobStateComponent>(mapCoords, ent.Comp1.WarningRange))
        {
            if (target.Owner == ent.Owner ||
                !ValidLizardTarget(target.Owner) ||
                !IsCarbonLikeMob(target.Owner) ||
                Faction.IsEntityFriendly(ent.Owner, target.Owner))
            {
                continue;
            }

            var targetCoords = Transform.GetMapCoordinates(target.Owner);
            var distance = (targetCoords.Position - mapCoords.Position).Length();
            if (distance <= ent.Comp1.AggroRange)
            {
                TryAggro(ent.Owner, target.Owner, ent.Comp1);
                PlayGrowl((ent.Owner, ent.Comp1));
                AlertPack(ent.Owner, target.Owner, ent.Comp1);
                return true;
            }

            if (distance >= warningDistance)
                continue;

            warning = target.Owner;
            warningDistance = distance;
        }

        if (warning == null || ent.Comp1.NextWarningAt > Timing.CurTime)
        {
            if (warning != null)
                WakeRest((ent.Owner, ent.Comp1));

            return warning != null;
        }

        ent.Comp1.NextWarningAt = Timing.CurTime + ent.Comp1.WarningCooldown;
        WakeRest((ent.Owner, ent.Comp1));
        PlayGrowl((ent.Owner, ent.Comp1));
        Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-warning"), ent.Owner, warning.Value, PopupType.MediumCaution);
        return true;
    }

    private bool ValidLizardTarget(EntityUid uid)
    {
        return !TerminatingOrDeleted(uid) &&
               MobQuery.HasComp(uid) &&
               !MobState.IsDead(uid);
    }
}
