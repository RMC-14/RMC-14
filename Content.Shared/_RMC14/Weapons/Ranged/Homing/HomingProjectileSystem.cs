using System.Numerics;
using Content.Shared.Projectiles;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._RMC14.Weapons.Ranged.Homing;

public sealed class HomingProjectileSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    /// <summary>
    ///     Adjust the velocity and rotation of the projectile every frame.
    /// </summary>
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<HomingProjectileComponent>();

        while (query.MoveNext(out var projectile, out var component))
        {
            if(!TryComp(projectile, out PhysicsComponent? physics))
                return;

            // Get the map coordinates and the direction
            var target = component.Target;
            var targetCoords = _transform.GetMapCoordinates(target, Transform(target));
            var projectileCoords = _transform.GetMapCoordinates(projectile, Transform(projectile));
            var direction = targetCoords.Position - projectileCoords.Position;

            // Get the velocity of the target and the projectile
            var targetMapVelocity = Vector2.Zero + direction.Normalized() * component.ProjectileSpeed;
            var currentMapVelocity = _physics.GetMapLinearVelocity(projectile, physics);

            // Adjust the velocity
            var newLinear = physics.LinearVelocity + targetMapVelocity - currentMapVelocity;

            _physics.SetLinearVelocity(projectile, newLinear, body: physics);

            if(!TryComp(projectile, out ProjectileComponent? projectileComponent))
                return;

            _transform.SetWorldRotationNoLerp(projectile, direction.ToWorldAngle() + projectileComponent.Angle);
        }
    }
}
