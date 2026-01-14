using System.Numerics;
using Content.Server.Explosion.EntitySystems;
using Content.Shared._RMC14.Explosion;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Explosion;

public sealed class HefaKnightsSystem : SharedHefaKnightsSystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    protected override void ExplodeSword(Entity<HefaKnightExplosionsComponent> ent, EntityUid user, EntityUid target)
    {
        var targetCoords = Transform(target).Coordinates;
        var epicenter = TransformSystem.GetMapCoordinates(target);

        var userRotation = TransformSystem.GetWorldRotation(user);
        var userDirection = userRotation.ToWorldVec().Normalized();

        PerformExplosion(targetCoords, epicenter, ent.Comp, user, userDirection);
        QueueDel(ent);
    }

    private void PerformExplosion(EntityCoordinates spawnCoords, MapCoordinates epicenter, HefaKnightExplosionsComponent explosion, EntityUid? user, Vector2? direction = null)
    {
        if (explosion.ExplosionEffect != null)
            Spawn(explosion.ExplosionEffect.Value, spawnCoords);

        if (explosion.ShockWaveEffect != null)
            Spawn(explosion.ShockWaveEffect.Value, spawnCoords);

        SpawnShrapnel(spawnCoords, explosion.ShrapnelProjectile, explosion.ShrapnelCount, direction);

        _explosion.QueueExplosion(
            epicenter,
            explosion.ExplosionType,
            explosion.TotalIntensity,
            explosion.IntensitySlope,
            explosion.MaxIntensity,
            user
        );
    }

    /// <summary>
    /// Spawns shrapnel projectiles. If direction is provided (sword), shrapnel is fired in a cone.
    /// If direction is null (helmet), shrapnel is fired in all directions.
    /// </summary>
    private void SpawnShrapnel(EntityCoordinates coords, string shrapnelPrototype, int count, Vector2? direction = null)
    {
        // CMSS13: Sword fires shrapnel in a ~90 degree cone in the facing direction
        // Helmet fires shrapnel in all 360 degrees
        const float coneAngle = MathF.PI / 2f; // 90 degree cone for directional

        for (var i = 0; i < count; i++)
        {
            Vector2 throwDirection;

            if (direction != null)
            {
                // Directional: spread shrapnel in a cone centered on the facing direction
                var baseAngle = MathF.Atan2(direction.Value.Y, direction.Value.X);
                var offset = _random.NextFloat(-coneAngle / 2f, coneAngle / 2f);
                var angle = baseAngle + offset;
                throwDirection = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 10;
            }
            else
            {
                // Omnidirectional: spread shrapnel in all directions
                var angle = _random.NextAngle();
                throwDirection = angle.ToVec().Normalized() * 10;
            }

            var shrapnel = Spawn(shrapnelPrototype, coords);
            _throwing.TryThrow(shrapnel, throwDirection, 0.5f);
        }
    }
}
