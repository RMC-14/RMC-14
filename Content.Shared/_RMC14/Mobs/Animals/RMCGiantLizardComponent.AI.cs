using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardComponent
{
    [DataField]
    public float AggroRange = 3f;

    [DataField]
    public float WarningRange = 5f;

    [DataField]
    public float TargetSearchRange = 16f;

    [DataField]
    public float PackAlertRange = 7f;

    [DataField]
    public TimeSpan RecentHitTime = TimeSpan.FromSeconds(5);

    [ViewVariables]
    public TimeSpan LastHitAt = TimeSpan.MinValue;

    [DataField]
    public TimeSpan AggressionMemory = TimeSpan.FromSeconds(30);

    [ViewVariables]
    public TimeSpan LastAggroAt = TimeSpan.MinValue;

    [DataField]
    public TimeSpan WarningCooldown = TimeSpan.FromSeconds(5);

    [ViewVariables]
    public TimeSpan NextWarningAt;

    [DataField]
    public float LowHealthRetreatFraction = 0.25f;

    [DataField]
    public float FightOrFlightHealthFraction = 0.66f;

    [DataField]
    public float RetreatSpeed = 4.5f;

    [DataField]
    public TimeSpan RetreatDuration = TimeSpan.FromSeconds(4.5);

    [DataField]
    public TimeSpan RetreatCooldown = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan RetreatRepathCooldown = TimeSpan.FromSeconds(0.5);

    [DataField]
    public int RetreatMaxAttempts = 2;

    [DataField]
    public float RetreatReattemptRange = 7f;

    [DataField]
    public TimeSpan RetreatReattemptDuration = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public bool Retreating;

    [ViewVariables]
    public int RetreatAttempts;

    [ViewVariables]
    public TimeSpan RetreatUntil;

    [ViewVariables]
    public TimeSpan NextRetreatAt;

    [ViewVariables]
    public TimeSpan NextRetreatMoveAt;

    [ViewVariables]
    public EntityUid? RetreatTarget;

    [DataField]
    public float SkirmishChance = 0.33f;

    [DataField]
    public float SkirmishSpeed = 4.5f;

    [DataField]
    public TimeSpan SkirmishDuration = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public bool Skirmishing;

    [ViewVariables]
    public TimeSpan SkirmishUntil;

    [ViewVariables]
    public EntityUid? SkirmishTarget;

    [DataField]
    public DamageSpecifier ObstacleDamage = new()
    {
        DamageDict = { { "Slash", FixedPoint2.New(30) } }
    };

    [DataField]
    public TimeSpan ObstacleAttackCooldown = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public TimeSpan NextObstacleAttackAt;
}
