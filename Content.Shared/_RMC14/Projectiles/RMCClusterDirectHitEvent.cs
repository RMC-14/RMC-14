namespace Content.Shared._RMC14.Projectiles;

/// <summary>
///     Raised on the projectile that does a direct hit with a projectile grenade
/// </summary>
/// <param name="DirectHitProjectile"> The newly spawned projectile from the grenade that will be used to calculate damage.</param>
/// <param name="Hit"></param>
/// <param name="Origin"></param>
[ByRefEvent]
public record struct RMCClusterDirectHitEvent(EntityUid DirectHitProjectile, EntityUid Hit, EntityUid Origin);
