using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._RMC14.Explosion;
using Content.Shared.Explosion.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Explosion;

public sealed class HefaSwordSplosionSystem : SharedHefaSwordSplosionSystem
{
    [Dependency] private readonly RMCExplosionSystem _rmcExplosion = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    // Reusable lists to avoid allocations during explosions.
    private readonly List<EntityUid> _spawned = new();
    private readonly List<EntityUid> _hitEntities = new();

    protected override void ExplodeSword(Entity<HefaSwordSplosionComponent> ent, EntityUid user, EntityUid target)
    {
        if (!TryComp(target, out TransformComponent? targetXform))
            return;
        if (!TryComp(ent, out ExplosiveComponent? explosive))
            return;

        var targetCoords = TransformSystem.GetMapCoordinates(targetXform);
        var userRotation = Transform(user).LocalRotation;

        SpawnShrapnel(ent, user, targetCoords, userRotation);
        _rmcExplosion.QueueExplosion(
            targetCoords,
            explosive.ExplosionType,
            explosive.TotalIntensity,
            explosive.IntensitySlope,
            explosive.MaxIntensity,
            user,
            explosive.TileBreakScale,
            explosive.MaxTileBreak,
            explosive.CanCreateVacuum);

        var ev = new CMExplosiveTriggeredEvent();
        RaiseLocalEvent(ent, ref ev);

        QueueDel(ent);
    }

    private void SpawnShrapnel(
        Entity<HefaSwordSplosionComponent> ent,
        EntityUid user,
        MapCoordinates coords,
        Angle userRotation)
    {
        var comp = ent.Comp;

        if (comp.ShrapnelCount <= 0)
            return;

        _spawned.Clear();
        _hitEntities.Clear();

        var shrapnelCount = comp.ShrapnelCount;
        var halfSpread = comp.SpreadAngle / 2;
        var segmentAngle = comp.SpreadAngle / shrapnelCount;

        // Convert user rotation to the direction they're facing.
        // Subtract 90 degrees because sprite rotation 0 points up (+Y), but we want forward direction.
        var baseAngle = userRotation.Degrees - 90 - halfSpread;

        for (var i = 0; i < shrapnelCount; i++)
        {
            var shrapnel = Spawn(comp.ShrapnelPrototype, coords);

            // Calculate angle for even distribution across the cone.
            var angle = Angle.FromDegrees(baseAngle + segmentAngle * (i + 0.5));
            var direction = angle.ToVec().Normalized();
            var velocity = _random.NextVector2(comp.MinVelocity, comp.MaxVelocity);

            // Shoot projectile with user as the shooter (not the sword).
            _gun.ShootProjectile(shrapnel, direction, velocity, user, user, comp.ProjectileSpeed);
            _spawned.Add(shrapnel);
        }

        // Raise cluster spawned event for ClusterLimitHits to work.
        var clusterEv = new CMClusterSpawnedEvent(_spawned, _hitEntities, user);
        RaiseLocalEvent(ent, ref clusterEv);
        RaiseLocalEvent(ent, new AmmoShotEvent { FiredProjectiles = _spawned });
    }
}
