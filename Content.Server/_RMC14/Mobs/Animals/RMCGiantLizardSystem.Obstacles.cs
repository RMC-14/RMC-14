using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.Damage;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private void TryBreakNearbyObstacle(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        if (ent.Comp1.NextObstacleAttackAt > Timing.CurTime)
            return;

        var coords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        foreach (var obstacle in Lookup.GetEntitiesInRange<DamageableComponent>(coords, 1.25f))
        {
            if (obstacle.Owner == ent.Owner ||
                MobQuery.HasComp(obstacle.Owner) ||
                ItemQuery.HasComp(obstacle.Owner) ||
                !XformQuery.TryGetComponent(obstacle.Owner, out var obstacleXform) ||
                !obstacleXform.Anchored)
            {
                continue;
            }

            Damageable.TryChangeDamage(obstacle.Owner, ent.Comp1.ObstacleDamage, origin: ent.Owner, tool: ent.Owner);
            ent.Comp1.NextObstacleAttackAt = Timing.CurTime + ent.Comp1.ObstacleAttackCooldown;
            return;
        }
    }
}
