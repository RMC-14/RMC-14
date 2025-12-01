using Content.Shared._RMC14.Maths;
using Content.Shared.FixedPoint;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

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
    public bool IsActivateSplashOnGib = true;

    [DataField]
    public float CloseSplashRadius = RMCMathExtensions.CircleAreaFromSquareSide(1);

    [DataField]
    public float StandardSplashRadius = RMCMathExtensions.CircleAreaFromSquareSide(3);

    [DataField]
    public float GibSplashRadius = RMCMathExtensions.CircleAreaFromSquareSide(5);

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
    /// Probability that target will scream after hit
    /// </summary>
    [DataField]
    public float TargetScreamProbability = 0.6f;

    /// <summary>
    /// How much probability increase if damage type is brute
    /// </summary>
    [DataField]
    public float BruteDamageProbabilityModificator = 0.05f;

    /// <summary>
    /// How much probability of trigger acid splash increase with additional damage
    /// </summary>
    [DataField]
    public float DamageTriggerProbabilityMultiplier = 0.33f;

    [DataField]
    public TimeSpan SplashCooldown = TimeSpan.FromSeconds(3);

    [DataField, AutoPausedField]
    public TimeSpan NextSplashAvailable;

    [DataField]
    public EntProtoId BloodDecalSpawnerPrototype = "RMCDecalSpawnerAcidBloodSplash";

    [DataField]
    public SoundSpecifier AcidSplashSound = new SoundCollectionSpecifier("XenoAcidSizzle");
}
