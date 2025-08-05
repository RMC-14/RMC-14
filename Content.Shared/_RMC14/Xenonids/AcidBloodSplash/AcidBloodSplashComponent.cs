using Robust.Shared.GameStates;
using Content.Shared.FixedPoint;
using Content.Shared.Damage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.AcidBloodSplash;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(AcidBloodSplashSystem))]
public sealed partial class AcidBloodSplashComponent : Component
{
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new();

    [DataField, AutoNetworkedField]
    public FixedPoint2 MinimalTriggerDamage = 12;

    [DataField, AutoNetworkedField]
    public float CloseSplashRadius = (float)(1 / Math.Sqrt(Math.PI)); // a circle equal in area to a square with a side of 1 (single tile)

    [DataField, AutoNetworkedField]
    public float StandardSplashRadius = (float)(3 / Math.Sqrt(Math.PI)); // a circle equal in area to a square with a side of 3 (3x3 tiles)

    [DataField, AutoNetworkedField]
    public float GibSplashRadius = (float)(5 / Math.Sqrt(Math.PI)); // a circle equal in area to a square with a side of 5 (5x5 tiles)

    /// <summary>
    /// Probability of trigger acid splash after minimal damage check, may be increased
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BaseSplashTriggerProbability = 0.2f;

    /// <summary>
    /// Probability that target will be hit after splash is activated, decreases with the number of targets
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BaseHitProbability = 0.65f;

    /// <summary>
    /// Substitution of gib chance
    /// </summary>
    // TODO: remove when xeno can be gibbed
    [DataField, AutoNetworkedField]
    public float BaseDeathSplashProbability = 0.05f;

    /// <summary>
    /// How much probability increase if damage type is brute
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BruteDamageProbabilityModificator = 0.05f;

    /// <summary>
    /// How much probability increase with additional damage
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DamageProbabilityMultiplier = 0.33f;

    [DataField, AutoNetworkedField]
    public TimeSpan SplashCooldown = TimeSpan.FromSeconds(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextSplashAvailable;
}
