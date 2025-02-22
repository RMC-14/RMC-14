using Content.Server.Explosion.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Projectiles;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Server.Explosion.EntitySystems;

public sealed class RMCProjectileGrenadeSystem : EntitySystem
{
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;
    [Dependency] private readonly ProjectileGrenadeSystem _projectileGrenade = default!;

    private readonly List<EntityUid> _spawned = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileGrenadeComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ProjectileGrenadeComponent, FragmentIntoProjectilesEvent>(OnFragmentIntoProjectiles);
    }

    /// <summary>
    /// Reverses the payload shooting direction if the projectile grenade collides with an entity
    /// </summary>
    private void OnStartCollide(Entity<ProjectileGrenadeComponent> entity, ref StartCollideEvent args)
    {
        if (!entity.Comp.Rebounds)
            return;

        //Shoot the payload backwards if colliding with an entity
        entity.Comp.DirectionAngle += entity.Comp.ReboundAngle;

        var ev = new RMCProjectileReboundEvent(entity.Comp.ReboundAngle);
        RaiseLocalEvent(entity, ref ev);

        _trigger.Trigger(entity);
    }

    /// <summary>
    /// Spawns projectiles at the coordinates of the grenade upon triggering
    /// Can customize the angle and velocity the projectiles come out at
    /// </summary>
    private void OnFragmentIntoProjectiles(Entity<ProjectileGrenadeComponent> ent, ref FragmentIntoProjectilesEvent args)
    {
        args.Handled = true;

        var grenadeCoord = _transformSystem.GetMapCoordinates(ent.Owner);
        var shootCount = 0;
        var totalCount = ent.Comp.Container.ContainedEntities.Count + ent.Comp.UnspawnedCount;
        var segmentAngle = ent.Comp.SpreadAngle / totalCount;
        var projectileRotation = _transformSystem.GetMoverCoordinateRotation(ent.Owner, Transform(ent.Owner)).worldRot.Degrees + ent.Comp.DirectionAngle;

        _spawned.Clear();
        while (_projectileGrenade.TrySpawnContents(grenadeCoord, ent.Comp, out var contentUid))
        {
            // Give the same IFF faction and enabled state to the projectiles shot from the grenade
            if (ent.Comp.InheritIFF)
            {
                if (TryComp(ent.Owner, out ProjectileIFFComponent? grenadeIFFComponent))
                {
                    _gunIFF.GiveAmmoIFF(contentUid, grenadeIFFComponent.Faction, grenadeIFFComponent.Enabled);
                }
            }

            var angleMin = projectileRotation - ent.Comp.SpreadAngle / 2 + segmentAngle * shootCount;
            var angleMax = projectileRotation - ent.Comp.SpreadAngle / 2 + segmentAngle * (shootCount + 1);

            Angle angle;
            if (ent.Comp.RandomAngle)
                angle = _random.NextAngle();
            else if (ent.Comp.EvenSpread)
                angle = Angle.FromDegrees((angleMin + angleMax) / 2);
            else
            {
                angle = Angle.FromDegrees(_random.Next((int)angleMin, (int)angleMax));
            }
            shootCount++;

            // velocity is randomized to make the projectiles look
            // slightly uneven, doesn't really change much, but it looks better
            var direction = angle.ToVec().Normalized();
            var velocity = _random.NextVector2(ent.Comp.MinVelocity, ent.Comp.MaxVelocity);
            _gun.ShootProjectile(contentUid, direction, velocity, ent.Owner, null, ent.Comp.ProjectileSpeed);
            _spawned.Add(contentUid);
        }

        var clusterEv = new CMClusterSpawnedEvent(_spawned);
        RaiseLocalEvent(ent.Owner, ref clusterEv);
        RaiseLocalEvent(ent.Owner,
            new AmmoShotEvent
            {
                FiredProjectiles = _spawned,
            });
        QueueDel(ent.Owner);
    }
}
