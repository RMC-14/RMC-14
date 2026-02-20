using Content.Shared._RMC14.Maths;
using Content.Shared.FixedPoint;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Server._RMC14.BloodSplash;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(BloodSplashSystem))]
public sealed partial class BloodSplashComponent : Component
{
    [DataField]
    public FixedPoint2 MinimalTriggerDamage = 12;

    [DataField]
    public bool WorksWhileDead = true;

    /// Probability of trigger blood splash after minimal damage check.
    /// This value is in percentage.
    [DataField]
    public float BaseSplashTriggerProbability = 30f;

    /// How much probability increase if damage type is brute
    /// This value is in percentage.
    [DataField]
    public float BruteDamageProbabilityModificator = 5f;

    /// How much probability of trigger blood splash increase with additional damage
    [DataField]
    public float DamageTriggerProbabilityMultiplier = 0.33f;

    [DataField]
    public TimeSpan SplashCooldown = TimeSpan.FromSeconds(3);

    [DataField, AutoPausedField]
    public TimeSpan NextSplashAvailable;

    [DataField]
    public EntProtoId BloodDecalSpawnerPrototype = "RMCDecalSpawnerBloodSplatters";
}
