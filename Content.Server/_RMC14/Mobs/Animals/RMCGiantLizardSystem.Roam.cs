using System.Numerics;
using Content.Shared._RMC14.Mobs.Animals;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private bool UpdateCalmRoam(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        var comp = ent.Comp1;
        if (!CanCalmRoam(ent))
        {
            if (comp.Roaming)
                StopRoam((ent.Owner, comp), false);

            return false;
        }

        var now = Timing.CurTime;
        if (comp.Roaming)
        {
            if (comp.RoamTarget is not { } target ||
                comp.RoamUntil <= now ||
                Transform.GetMoverCoordinates(ent.Owner).TryDistance(EntityManager, target, out var distance) && distance <= comp.RoamArriveRange)
            {
                StopRoam((ent.Owner, comp));
                return true;
            }

            if (comp.NextRoamMoveAt > now)
                return true;

            comp.NextRoamMoveAt = now + comp.RoamRepathCooldown;
            if (!TryMoveTowards(ent.Owner, target, comp.RoamSpeed))
                StopRoam((ent.Owner, comp));

            return true;
        }

        if (comp.NextRoamAt > now)
            return false;

        if (!TryPickRoamTarget(ent, out var roamTarget))
        {
            comp.NextRoamAt = now + RandomTime(comp.RoamPauseMin, comp.RoamPauseMax);
            return false;
        }

        StartRoam((ent.Owner, comp), roamTarget);
        return true;
    }

    private bool CanCalmRoam(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        return !ent.Comp1.Resting &&
               !ent.Comp1.Leaping &&
               !ent.Comp1.Retreating &&
               !ent.Comp1.Skirmishing &&
               ent.Comp1.RavageTarget == null &&
               ent.Comp1.FoodTarget == null &&
               !ent.Comp1.EatingFood &&
               !IsOnFire(ent.Owner) &&
               !WasRecentLizardTime(ent.Comp1.LastHitAt, ent.Comp1.CalmRestDelay);
    }

    private void StartRoam(Entity<RMCGiantLizardComponent> ent, EntityCoordinates target)
    {
        ent.Comp.Roaming = true;
        ent.Comp.RoamTarget = target;
        ent.Comp.RoamUntil = Timing.CurTime + RandomTime(ent.Comp.RoamMoveDurationMin, ent.Comp.RoamMoveDurationMax);
        ent.Comp.NextRoamMoveAt = TimeSpan.Zero;
    }

    private void StopRoam(Entity<RMCGiantLizardComponent> ent, bool pause = true)
    {
        if (!ent.Comp.Roaming && ent.Comp.RoamTarget == null)
            return;

        ent.Comp.Roaming = false;
        ent.Comp.RoamTarget = null;
        ent.Comp.RoamUntil = TimeSpan.Zero;
        ent.Comp.NextRoamMoveAt = TimeSpan.Zero;
        StopMovement(ent.Owner);

        if (pause)
            ent.Comp.NextRoamAt = Timing.CurTime + RandomTime(ent.Comp.RoamPauseMin, ent.Comp.RoamPauseMax);
    }

    private bool TryPickRoamTarget(Entity<RMCGiantLizardComponent, TransformComponent> ent, out EntityCoordinates target)
    {
        var current = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        var origin = current.Position;

        var center = Vector2.Zero;
        var nearest = Vector2.Zero;
        var nearestDistance = float.MaxValue;
        var packCount = 0;

        foreach (var lizard in Lookup.GetEntitiesInRange<RMCGiantLizardComponent>(current, ent.Comp1.RoamPackSearchRange))
        {
            if (lizard.Owner == ent.Owner ||
                ActorQuery.HasComp(lizard.Owner) ||
                !MobState.IsAlive(lizard.Owner) ||
                !XformQuery.TryGetComponent(lizard.Owner, out var lizardXform))
            {
                continue;
            }

            var lizardCoords = Transform.GetMapCoordinates((lizard.Owner, lizardXform));
            if (lizardCoords.MapId != current.MapId)
                continue;

            var position = lizardCoords.Position;
            var distance = (position - origin).Length();

            center += position;
            packCount++;

            if (distance >= nearestDistance)
                continue;

            nearest = position;
            nearestDistance = distance;
        }

        if (packCount > 0)
        {
            if (nearestDistance < ent.Comp1.RoamPackSeparationDistance)
            {
                var away = origin - nearest;
                if (away.LengthSquared() < 0.01f)
                    away = Random.NextAngle().RotateVec(Vector2.UnitX);

                target = ent.Comp2.Coordinates.Offset(away.Normalized() * ent.Comp1.RoamSoloRadius * 0.75f + RandomRoamJitter(ent.Comp1));
                return true;
            }

            var packCenter = center / packCount;
            var toPack = packCenter - origin;
            if (toPack.Length() > ent.Comp1.RoamPackJoinDistance ||
                Random.Prob(ent.Comp1.RoamPackCenterChance))
            {
                target = ent.Comp2.Coordinates.Offset(LimitRoamOffset(toPack, ent.Comp1.RoamSoloRadius) + RandomRoamJitter(ent.Comp1));
                return true;
            }
        }

        target = ent.Comp2.Coordinates.Offset(Random.NextAngle().RotateVec(Vector2.UnitX) * Random.NextFloat(1.25f, ent.Comp1.RoamSoloRadius));
        return true;
    }

    private Vector2 RandomRoamJitter(RMCGiantLizardComponent comp)
    {
        return Random.NextAngle().RotateVec(Vector2.UnitX) * Random.NextFloat(0f, comp.RoamPackTargetJitter);
    }

    private static Vector2 LimitRoamOffset(Vector2 offset, float maxLength)
    {
        if (offset.LengthSquared() < 0.01f)
            return offset;

        var length = MathF.Min(offset.Length(), maxLength);
        return offset.Normalized() * length;
    }
}
