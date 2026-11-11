namespace Content.Shared._RMC14.Chemistry;

[RegisterComponent]
public sealed partial class RMCChemVolatilesComponent : Component
{
    [DataField]
    public int MinFireRadius = 1;

    [DataField]
    public int MaxFireRadius = 5;

    [DataField]
    public int MinFireIntensity = 3;

    [DataField]
    public int MaxFireIntensity = 20;

    [DataField]
    public int MinFireDuration = 3;

    [DataField]
    public int MaxFireDuration = 24;

    [DataField]
    public float PhosphorusPerSmokeRadius = 10;

    [DataField]
    public float MaxExplosivePower = 175;

    [DataField]
    public float BaseExplosiveFalloff = 75;

    [DataField]
    public float MinExplosiveFalloff = 25;

    [DataField]
    public float PowerPerIntensity = 5;

    [DataField]
    public int BaseShards = 4;

    [DataField]
    public int MaxShards = 32;

    [DataField]
    public float SpecialShardThreshold = 10;

    [DataField]
    public float ShardDirectHitChance = 0.15f;

    [DataField]
    public float ShrapnelSpeed = 20;

    [DataField]
    public TimeSpan ExplodedCooldown = TimeSpan.FromSeconds(2);
}
