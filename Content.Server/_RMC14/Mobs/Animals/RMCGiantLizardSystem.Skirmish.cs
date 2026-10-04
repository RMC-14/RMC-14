using Content.Shared._RMC14.Mobs.Animals;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private void StartSkirmish(Entity<RMCGiantLizardComponent> ent, EntityUid target)
    {
        if (ActorQuery.HasComp(ent.Owner) ||
            ent.Comp.Leaping ||
            ent.Comp.RavageTarget != null ||
            !ValidLizardTarget(target))
        {
            return;
        }

        WakeRest(ent);
        StopRoam(ent, false);
        ent.Comp.Skirmishing = true;
        ent.Comp.SkirmishTarget = target;
        ent.Comp.SkirmishUntil = Timing.CurTime + ent.Comp.SkirmishDuration;
        TryMoveAwayFrom(ent.Owner, Transform.GetMoverCoordinates(target), ent.Comp.SkirmishSpeed);
    }

    private bool UpdateSkirmish(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        if (!ent.Comp1.Skirmishing)
            return false;

        if (ent.Comp1.SkirmishUntil <= Timing.CurTime ||
            ent.Comp1.SkirmishTarget is not { } target ||
            !ValidLizardTarget(target))
        {
            StopSkirmish((ent.Owner, ent.Comp1));
            return false;
        }

        var targetCoordinates = Transform.GetMoverCoordinates(target);
        if (!Transform.GetMoverCoordinates(ent.Owner).TryDistance(EntityManager, targetCoordinates, out var distance) ||
            distance > ent.Comp1.SkirmishMaxRange)
        {
            StopSkirmish((ent.Owner, ent.Comp1));
            return false;
        }

        TryMoveAwayFrom(ent.Owner, targetCoordinates, ent.Comp1.SkirmishSpeed);
        return true;
    }

    private void StopSkirmish(Entity<RMCGiantLizardComponent> ent)
    {
        ent.Comp.Skirmishing = false;
        ent.Comp.SkirmishTarget = null;
        StopMovement(ent.Owner);
    }
}
