// Resharper disable once CheckNameSpace
namespace Content.Server.Explosion.Components;

/// <summary>
/// Extends the upstream ProjectileGrenadeComponent
/// </summary>
public sealed partial class ProjectileGrenadeComponent
{
    /// <summary>
    ///     Decides if the grenade will shoot it's payload backwards when colliding.
    /// </summary>
    [DataField]
    public bool Rebounds;

    /// <summary>
    ///     Adjust the shooting direction, -90 is the front, 90 is the back.
    /// </summary>
    [DataField]
    public float DirectionAngle = -90;

    /// <summary>
    ///     How many seconds after rebounding the projectile should be triggered.
    /// </summary>
    [DataField]
    public float ReboundTimer = 0.05f;

    /// <summary>
    ///     The angle of the projectile spray
    /// </summary>
    [DataField]
    public float SpreadAngle = 360;

    /// <summary>
    ///     Determines if the spread will be uniform.
    /// </summary>
    [DataField]
    public bool EvenSpread;

    /// <summary>
    ///     The speed of the projectile
    /// </summary>
    [DataField]
    public float ProjectileSpeed = 20f;

    /// <summary>
    ///     If the projectiles spawned from the grenade will inherit the IFF targeting from the projectile grenade
    /// </summary>
    [DataField]
    public bool InheritIFF;

    [DataField]
    public bool DirectHit;

    /// <summary>
    ///     Chance for each payload projectile to directly hit a mob at the fragmentation origin.
    ///     If unset, <see cref="DirectHitProjectiles"/> is used instead.
    /// </summary>
    [DataField]
    public float? DirectHitChance;

    /// <summary>
    ///     Distance in tiles from the grenade at which its payload is spawned.
    /// </summary>
    [DataField]
    public float SpawnOffset;

    /// <summary>
    ///     Whether the user from the trigger event should be assigned as the payload's shooter.
    /// </summary>
    [DataField]
    public bool TriggerUserIsShooter;

    /// <summary>
    ///     Aligns the spawn offset to a tile step instead of using a normalized direction.
    /// </summary>
    [DataField]
    public bool TileAlignedSpawnOffset;

    /// <summary>
    ///     Random variance applied to the payload projectile speed.
    /// </summary>
    [DataField]
    public float ProjectileSpeedVariance;

    /// <summary>
    ///     Minimum damage multiplier rolled independently for each payload projectile.
    /// </summary>
    [DataField]
    public float MinProjectileDamageMultiplier = 1f;

    /// <summary>
    ///     Maximum damage multiplier rolled independently for each payload projectile.
    /// </summary>
    [DataField]
    public float MaxProjectileDamageMultiplier = 1f;
}
