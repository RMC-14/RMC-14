namespace Content.Shared._RMC14.Projectiles.Aimed;

[RegisterComponent]
public sealed partial class AimedShotEffectComponent : Component
{
    [DataField]
    public float BonusDamageMultiplier = 1f;

    [DataField]
    public int FireStacksOnHit;

    [DataField]
    public TimeSpan BlindDuration;

    [DataField]
    public TimeSpan SlowDuration;

    [DataField]
    public TimeSpan SuperSlowDuration;
}
