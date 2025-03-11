namespace Content.Shared._RMC14.Projectiles.Aimed;

[RegisterComponent]
public sealed partial class AimedShotEffectComponent : Component
{
    /// <summary>
    ///     The amount of times the base projectile damage should be repeated on the hit target.
    /// </summary>
    [DataField]
    public float ExtraHits;

    /// <summary>
    ///     The amount of fire stacks to apply on the hit target.
    /// </summary>
    [DataField]
    public int FireStacksOnHit;

    /// <summary>
    ///     The duration of the blind on the hit target.
    /// </summary>
    [DataField]
    public TimeSpan BlindDuration;

    /// <summary>
    ///     The duration of the slow on the hit target.
    /// </summary>
    [DataField]
    public TimeSpan SlowDuration;

    /// <summary>
    ///     The duration of the super sloow on the hit target.
    /// </summary>
    [DataField]
    public TimeSpan SuperSlowDuration;
}
