using Content.Shared.FixedPoint;
using Content.Shared.Damage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Server.Spawners.Components;

namespace Content.Server._RMC14.Xenonids.AcidBloodSplash;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(AcidBloodSplashSystem))]
public sealed partial class AcidBloodSplashComponent : Component
{
    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public FixedPoint2 MinimalTriggerDamage = 12;

    [DataField]
    public float CloseSplashRadius = (float)(1 / Math.Sqrt(Math.PI)); // a circle equal in area to a square with a side of 1 (single tile)

    [DataField]
    public float StandardSplashRadius = (float)(3 / Math.Sqrt(Math.PI)); // a circle equal in area to a square with a side of 3 (3x3 tiles)

    [DataField]
    public float GibSplashRadius = (float)(5 / Math.Sqrt(Math.PI)); // a circle equal in area to a square with a side of 5 (5x5 tiles)

    /// <summary>
    /// Probability of trigger acid splash after minimal damage check, may be increased
    /// </summary>
    [DataField]
    public float BaseSplashTriggerProbability = 0.2f;

    /// <summary>
    /// Probability that target will be hit after splash is activated, decreases with the number of targets
    /// </summary>
    [DataField]
    public float BaseHitProbability = 0.65f;

    /// <summary>
    /// Substitution of gib chance
    /// </summary>
    // TODO: remove when xeno can be gibbed
    [DataField]
    public float BaseDeathSplashProbability = 0.05f;

    /// <summary>
    /// How much probability increase if damage type is brute
    /// </summary>
    [DataField]
    public float BruteDamageProbabilityModificator = 0.05f;

    /// <summary>
    /// How much probability increase with additional damage
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DamageProbabilityMultiplier = 0.33f;

    [DataField, AutoNetworkedField]
    public TimeSpan SplashCooldown = TimeSpan.FromSeconds(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSplashAvailable;

    //[DataField, AutoNetworkedField]
    //public Entity<RandomDecalSpawnerComponent> BloodSpawner;
}
