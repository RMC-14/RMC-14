using System.Numerics;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._RMC14.Weapons.Ranged.Homing;

public sealed class HomingProjectileSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private readonly List<EntityUid> _toRemove = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<HomingShotsComponent, AmmoShotEvent>(OnAmmoShot);

        SubscribeLocalEvent<HomingProjectileComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<HomingProjectileComponent, PreventCollideEvent>(OnPreventCollide);
    }

    /// <summary>
    ///     Remove homing when colliding with the target.
    /// </summary>
    private void OnStartCollide(Entity<HomingProjectileComponent> ent, ref StartCollideEvent args)
    {
        if(args.OtherEntity != ent.Comp.Target)
            return;

        RemComp<HomingProjectileComponent>(ent);
    }

    /// <summary>
    ///     Remove homing if the collision with the target is prevented.
    /// </summary>
    private void OnPreventCollide(Entity<HomingProjectileComponent> ent, ref PreventCollideEvent args)
    {
        if(args.OtherEntity != ent.Comp.Target)
            return;

        RemComp<HomingProjectileComponent>(ent);
    }

    /// <summary>
    ///     Makes the shot ammo homing if it was targeted at a specific entity.
    /// </summary>
    private void OnAmmoShot(Entity<HomingShotsComponent> ent, ref AmmoShotEvent args)
    {
        foreach (var projectile in args.FiredProjectiles)
        {
            if(!TryComp(projectile, out TargetedProjectileComponent? targeted))
                return;

            var homing = EnsureComp<HomingProjectileComponent>(projectile);
            if (TryComp(ent, out GunComponent? gun))
                homing.ProjectileSpeed = gun.ProjectileSpeedModified;

            homing.Target = targeted.Target;
            Dirty(projectile, homing);
        }
    }

    /// <summary>
    ///     Adjust the velocity and rotation of the projectile every frame.
    /// </summary>
    public override void Update(float frameTime)
    {
        _toRemove.Clear();
        var query = EntityQueryEnumerator<HomingProjectileComponent>();
        while (query.MoveNext(out var projectile, out var component))
        {
            if(!TryComp(projectile, out PhysicsComponent? physics))
                continue;

            // Get the map coordinates and the direction
            var target = component.Target;
            var targetCoords = _transform.GetMapCoordinates(target, Transform(target));
            var projectileCoords = _transform.GetMapCoordinates(projectile, Transform(projectile));
            if (targetCoords.MapId != projectileCoords.MapId)
            {
                _toRemove.Add(projectile);
                continue;
            }

            var direction = targetCoords.Position - projectileCoords.Position;

            // Remove the homing component once the projectile gets close to it's target.
            if (_transform.InRange(Transform(projectile).Coordinates, Transform(target).Coordinates, 1f))
            {
                _toRemove.Add(projectile);
                continue;
            }

            // Get the velocity of the target and the projectile
            var targetMapVelocity = Vector2.Zero + direction.Normalized() * component.ProjectileSpeed;
            var currentMapVelocity = _physics.GetMapLinearVelocity(projectile, physics);

            // Adjust the velocity
            var newLinear = physics.LinearVelocity + targetMapVelocity - currentMapVelocity;

            _physics.SetLinearVelocity(projectile, newLinear, body: physics);

            if (!TryComp(projectile, out ProjectileComponent? projectileComponent))
                continue;

            _transform.SetWorldRotationNoLerp(projectile, direction.ToWorldAngle() + projectileComponent.Angle);
        }

        try
        {
            foreach (var remove in _toRemove)
            {
                RemComp<HomingProjectileComponent>(remove);
            }
        }
        finally
        {
            _toRemove.Clear();
        }
    }
}
