namespace Content.Shared._RMC14.Projectiles;

/// <summary>
/// Used in RMC before accuracy would be calculated, for guaranteed projectile dodges etc
/// </summary>
[ByRefEvent]
public record struct RMCBeforeProjectileAccuracyEvent(EntityUid Projectile, bool GuaranteedMiss = false);
