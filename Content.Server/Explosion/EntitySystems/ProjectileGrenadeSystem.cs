using Content.Server._RMC14.Explosion;
using Content.Server.Explosion.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._RMC14.Explosion;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Explosion.EntitySystems;

public sealed class ProjectileGrenadeSystem : EntitySystem
{
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    // RMC14
    private readonly List<EntityUid> _spawned = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileGrenadeComponent, ComponentInit>(OnFragInit);
        SubscribeLocalEvent<ProjectileGrenadeComponent, ComponentStartup>(OnFragStartup);
        SubscribeLocalEvent<ProjectileGrenadeComponent, TriggerEvent>(OnFragTrigger);
    }

    private void OnFragInit(Entity<ProjectileGrenadeComponent> entity, ref ComponentInit args)
    {
        entity.Comp.Container = _container.EnsureContainer<Container>(entity.Owner, "cluster-payload");
    }

    /// <summary>
    /// Setting the unspawned count based on capacity so we know how many new entities to spawn
    /// </summary>
    private void OnFragStartup(Entity<ProjectileGrenadeComponent> entity, ref ComponentStartup args)
    {
        if (entity.Comp.FillPrototype == null)
            return;

        entity.Comp.UnspawnedCount = Math.Max(0, entity.Comp.Capacity - entity.Comp.Container.ContainedEntities.Count);
    }

    /// <summary>
    /// Can be triggered either by damage or the use in hand timer
    /// </summary>
    private void OnFragTrigger(Entity<ProjectileGrenadeComponent> entity, ref TriggerEvent args)
    {
        FragmentIntoProjectiles(entity.Owner, entity.Comp, args.User);
        args.Handled = true;
    }

    /// <summary>
    /// Spawns projectiles at the coordinates of the grenade upon triggering
    /// Can customize the angle and velocity the projectiles come out at
    /// </summary>
    private void FragmentIntoProjectiles(EntityUid uid, ProjectileGrenadeComponent component, EntityUid? user)
    {
        var grenadeCoord = _transformSystem.GetMapCoordinates(uid);
        var projectileRotation = _transformSystem.GetWorldRotation(uid) + Angle.FromDegrees(component.DirectionAngle);
        var spawnDirection = projectileRotation.ToVec();
        if (component.TileAlignedSpawnOffset)
        {
            var largestAxis = MathF.Max(MathF.Abs(spawnDirection.X), MathF.Abs(spawnDirection.Y));
            if (largestAxis > 0)
                spawnDirection /= largestAxis;
        }

        var spawnCoordinates = new MapCoordinates(
            grenadeCoord.Position + spawnDirection * component.SpawnOffset,
            grenadeCoord.MapId);
        var totalCount = component.Container.ContainedEntities.Count + component.UnspawnedCount;

        // RMC14 it was sometimes dividing by 0.
        if (totalCount <= 0)
            return;

        if (!TrySpawnContents(spawnCoordinates, component, out var firstContentUid))
            return;

        var shooter = component.TriggerUserIsShooter ? user : null;
        var prepareEv = new PrepareFragmentIntoProjectilesEvent(firstContentUid, totalCount, spawnCoordinates, shooter);
        RaiseLocalEvent(uid, ref prepareEv);

        prepareEv.ConsumedProjectileIndices.RemoveWhere(index => index < 0 || index >= totalCount);
        var consumed = prepareEv.ConsumedProjectileIndices.Count;
        EntityUid? pendingContentUid = firstContentUid;
        if (consumed > 0)
        {
            QueueDel(firstContentUid);
            ConsumeContents(component, consumed - 1);
            pendingContentUid = null;
        }

        var hitEntities = prepareEv.HitEntities;
        var segmentAngle = 360 / totalCount;
        var shootCount = 0;

        _spawned.Clear();
        while (true)
        {
            while (prepareEv.ConsumedProjectileIndices.Contains(shootCount))
                shootCount++;

            if (shootCount >= totalCount)
                break;

            EntityUid contentUid;
            if (pendingContentUid is { } pending)
            {
                contentUid = pending;
                pendingContentUid = null;
            }
            else if (!TrySpawnContents(spawnCoordinates, component, out contentUid))
            {
                break;
            }

            Angle angle;
            if (component.RandomAngle)
                angle = _random.NextAngle();
            else
            {
                var angleMin = segmentAngle * shootCount;
                var angleMax = segmentAngle * (shootCount + 1);
                angle = Angle.FromDegrees(_random.Next(angleMin, angleMax));

                // RMC14
                var ev = new FragmentIntoProjectilesEvent(contentUid, totalCount, angle, shootCount, hitEntities);
                RaiseLocalEvent(uid, ref ev);

                if (ev.TotalCount <= 0)
                    return;
                if (ev.Handled)
                {
                    hitEntities = ev.HitEntities;
                    angle = ev.Angle;
                }
            }

            shootCount++;

            // RMC14
            EntityUid? gunUid = shooter == null ? null : uid;
            var shooterUid = shooter;

            if (TryComp(uid, out ProjectileComponent? clusterProjectile))
            {
                gunUid = clusterProjectile.Weapon;
                shooterUid = clusterProjectile.Shooter;
            }

            // velocity is randomized to make the projectiles look
            // slightly uneven, doesn't really change much, but it looks better
            var direction = angle.ToVec().Normalized();
            var velocity = _random.NextVector2(component.MinVelocity, component.MaxVelocity);
            var speedVariance = Math.Clamp(component.ProjectileSpeedVariance, 0, 1);
            var speed = component.ProjectileSpeed;
            if (speedVariance > 0)
                speed *= _random.NextFloat(1 - speedVariance, 1 + speedVariance);

            var minDamageMultiplier = MathF.Max(0, MathF.Min(
                component.MinProjectileDamageMultiplier,
                component.MaxProjectileDamageMultiplier));
            var maxDamageMultiplier = MathF.Max(0, MathF.Max(
                component.MinProjectileDamageMultiplier,
                component.MaxProjectileDamageMultiplier));
            if (TryComp(contentUid, out ProjectileComponent? projectile))
            {
                var damageMultiplier = minDamageMultiplier;
                if (minDamageMultiplier != maxDamageMultiplier)
                    damageMultiplier = _random.NextFloat(minDamageMultiplier, maxDamageMultiplier);

                if (damageMultiplier != 1f)
                    projectile.Damage *= damageMultiplier;
            }

            _gun.ShootProjectile(contentUid, direction, velocity, gunUid, shooterUid, speed);
            _spawned.Add(contentUid);
        }

        var clusterEv = new CMClusterSpawnedEvent(_spawned, hitEntities, uid);
        RaiseLocalEvent(uid, ref clusterEv);
        RaiseLocalEvent(uid,
            new AmmoShotEvent
            {
                FiredProjectiles = _spawned,
            });
        QueueDel(uid);
    }

    private void ConsumeContents(ProjectileGrenadeComponent component, int count)
    {
        while (count > 0 && component.UnspawnedCount > 0)
        {
            component.UnspawnedCount--;
            count--;
        }

        while (count > 0 && component.Container.ContainedEntities.Count > 0)
        {
            var contentUid = component.Container.ContainedEntities[0];
            if (!_container.Remove(contentUid, component.Container))
                break;

            QueueDel(contentUid);
            count--;
        }
    }

    /// <summary>
    /// Spawns one instance of the fill prototype or contained entity at the coordinate indicated
    /// </summary>
    private bool TrySpawnContents(MapCoordinates spawnCoordinates, ProjectileGrenadeComponent component, out EntityUid contentUid)
    {
        contentUid = default;

        if (component.UnspawnedCount > 0)
        {
            component.UnspawnedCount--;
            contentUid = Spawn(component.FillPrototype, spawnCoordinates);
            return true;
        }

        if (component.Container.ContainedEntities.Count > 0)
        {
            contentUid = component.Container.ContainedEntities[0];

            if (!_container.Remove(contentUid, component.Container))
                return false;

            _transformSystem.SetMapCoordinates(contentUid, spawnCoordinates);

            return true;
        }

        return false;
    }
}
