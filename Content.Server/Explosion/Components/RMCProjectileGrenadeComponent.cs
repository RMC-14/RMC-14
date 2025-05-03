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
    ///     The degrees added to the direction angle when the grenade rebounds
    /// </summary>
    [DataField]
    public float ReboundAngle = 180;

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

}
