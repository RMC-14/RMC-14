using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Item;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Mobs.Animals;

[RegisterComponent]
public sealed partial class RMCGiantLizardComponent : Component
{
    [DataField]
    public EntProtoId<WorldTargetActionComponent> PounceAction = "RMCActionGiantLizardPounce";

    [ViewVariables]
    public EntityUid? PounceActionEntity;

    [DataField]
    public float AggroRange = 3f;

    [DataField]
    public float WarningRange = 5f;

    [DataField]
    public float TargetSearchRange = 16f;

    [DataField]
    public float PackAlertRange = 7f;

    [DataField]
    public float MinPounceRange = 1f;

    [DataField]
    public float MaxPounceRange = 5f;

    [DataField]
    public TimeSpan PounceCooldown = TimeSpan.FromSeconds(9);

    [ViewVariables]
    public TimeSpan NextPounceAt;

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

    [ViewVariables]
    public bool Retreating;

    [ViewVariables]
    public TimeSpan RetreatUntil;

    [ViewVariables]
    public TimeSpan NextRetreatAt;

    [ViewVariables]
    public TimeSpan NextRetreatMoveAt;

    [ViewVariables]
    public EntityUid? RetreatTarget;

    [DataField]
    public float RestHealFraction = 0.05f;

    [DataField]
    public float AiFeedHealFraction = 0.15f;

    [DataField]
    public float AiFeedRange = 1.5f;

    [DataField]
    public float FoodSearchRange = 6f;

    [DataField]
    public float FoodTargetKeepRange = 5f;

    [ViewVariables]
    public EntityUid? FoodTarget;

    [ViewVariables]
    public bool EatingFood;

    [ViewVariables]
    public int FoodBitesLeft;

    [ViewVariables]
    public TimeSpan NextFoodBiteAt;

    [ViewVariables]
    public TimeSpan NextFoodSearchAt;

    [DataField]
    public int FoodBitesMin = 4;

    [DataField]
    public int FoodBitesMax = 6;

    [DataField]
    public TimeSpan FoodBiteDelayMin = TimeSpan.FromSeconds(1.7);

    [DataField]
    public TimeSpan FoodBiteDelayMax = TimeSpan.FromSeconds(2.5);

    [DataField]
    public TimeSpan FoodLostCooldown = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan FoodEatenCooldown = TimeSpan.FromSeconds(30);

    [DataField]
    public float FoodTheftRetaliateRange = 2f;

    [DataField]
    public float AiFeedTameRange = 7f;

    [DataField]
    public SoundSpecifier EatingSound = new SoundCollectionSpecifier("eating", AudioParams.Default.WithVolume(-4));

    [DataField]
    public float ForageSpeed = 2.5f;

    [DataField]
    public float DirectFeedHealFraction = 0.10f;

    [DataField]
    public TimeSpan UpdateCooldown = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public TimeSpan NextUpdateAt;

    [DataField]
    public TimeSpan CalmRestDelay = TimeSpan.FromSeconds(10);

    [ViewVariables]
    public bool Resting;

    [ViewVariables]
    public bool SleepingForRest;

    [DataField]
    public float FirePanicSpeed = 5f;

    [DataField]
    public float FireExtinguishChance = 0.15f;

    [DataField]
    public TimeSpan FirePanicCooldown = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public TimeSpan NextFirePanicAt;

    [DataField]
    public int PounceStrength = 20;

    [ViewVariables]
    public bool Leaping;

    [ViewVariables]
    public EntityCoordinates PounceOrigin;

    [ViewVariables]
    public TimeSpan PounceEndAt;

    [ViewVariables]
    public EntityUid? PounceTarget;

    [DataField]
    public DamageSpecifier PounceDamage = new()
    {
        DamageDict = { { "Blunt", FixedPoint2.New(8) } }
    };

    [DataField]
    public TimeSpan PounceKnockdown = TimeSpan.FromSeconds(2.5);

    [DataField]
    public TimeSpan PounceBlockedKnockdown = TimeSpan.FromSeconds(1.5);

    [DataField]
    public TimeSpan PounceObstacleKnockdown = TimeSpan.FromSeconds(1);

    [DataField]
    public float PounceBlockedKnockback = 1.25f;

    [DataField]
    public float PounceBlockedKnockbackSpeed = 10f;

    [DataField]
    public DamageSpecifier PounceObstacleDamage = new()
    {
        DamageDict = { { "Blunt", FixedPoint2.New(10) } }
    };

    [ViewVariables]
    public EntityUid? RavageTarget;

    [ViewVariables]
    public int RavageHitsLeft;

    [ViewVariables]
    public TimeSpan NextRavageAt;

    [DataField]
    public int RavageHitCount = 3;

    [DataField]
    public TimeSpan RavageHitDelay = TimeSpan.FromSeconds(0.4);

    [DataField]
    public TimeSpan RavageKnockdown = TimeSpan.FromSeconds(1.5);

    [DataField]
    public TimeSpan RavageDaze = TimeSpan.FromSeconds(1.5);

    [DataField]
    public int RavageCameraShakeStrength = 1;

    [DataField]
    public TimeSpan RavageCooldownRefund = TimeSpan.FromSeconds(3);

    [DataField]
    public DamageSpecifier RavageDamage = new()
    {
        DamageDict = { { "Slash", FixedPoint2.New(9) } }
    };

    [DataField]
    public DamageSpecifier XenoBonusDamage = new()
    {
        DamageDict = { { "Slash", FixedPoint2.New(8) } }
    };

    [DataField]
    public DamageSpecifier MeleeXenoBonusDamage = new()
    {
        DamageDict = { { "Slash", FixedPoint2.New(7) } }
    };

    [DataField]
    public SoundSpecifier BiteAttackSound = new SoundCollectionSpecifier("RMCGiantLizardBite", AudioParams.Default.WithVolume(-2));

    [DataField]
    public SoundSpecifier SlashAttackSound = new SoundCollectionSpecifier("RMCGiantLizardSlash", AudioParams.Default.WithVolume(-2));

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

    [DataField]
    public HashSet<string> AllowedTameFactions = new()
    {
        "UNMC",
        "SPP",
        "Halcyon",
        "WeYa",
        "Civilian",
        "CLF",
        "TSE",
        "HEFA",
        "RoyalMarines",
        "Bureau",
    };

    [DataField]
    public HashSet<string> ExcludedTameFactions = new()
    {
        "RMCXeno",
        "RMCDumb",
        "SimpleNeutral",
        "SimpleHostile",
        "Mouse",
        "PetsNT",
    };

    [DataField]
    public SoundSpecifier GrowlSound = new SoundCollectionSpecifier("RMCGiantLizardGrowl", AudioParams.Default.WithVolume(1));

    [DataField]
    public SoundSpecifier HissSound = new SoundCollectionSpecifier("RMCGiantLizardHiss", AudioParams.Default.WithVolume(-2));

    [DataField]
    public TimeSpan GrowlCooldownMin = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan GrowlCooldownMax = TimeSpan.FromSeconds(14);

    [ViewVariables]
    public TimeSpan NextGrowlAt;

    [DataField]
    public float TongueFlickChance = 0.25f;

    [DataField]
    public TimeSpan TongueFlickCooldown = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan TongueFlickDuration = TimeSpan.FromSeconds(0.3);

    [ViewVariables]
    public TimeSpan NextTongueFlickAt;

    [ViewVariables]
    public TimeSpan TongueFlickEndAt;

    [ViewVariables]
    public bool TongueVisible;

    [DataField]
    public float FriendlyPetRestChance = 0.15f;

    [DataField]
    public float FriendlyPetHissChance = 0.5f;

    [DataField]
    public TimeSpan FriendlyPetEmoteCooldownMin = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan FriendlyPetEmoteCooldownMax = TimeSpan.FromSeconds(8);

    [ViewVariables]
    public TimeSpan NextFriendlyPetEmoteAt;

    [DataField]
    public float DisarmKnockdownChance = 0.25f;

    [DataField]
    public TimeSpan DisarmKnockdown = TimeSpan.FromSeconds(0.4);

    [DataField]
    public SoundSpecifier DisarmKnockdownSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/alien_knockdown.ogg", AudioParams.Default.WithVolume(-4));

    [DataField]
    public float SmallWoundHealthFraction = 0.75f;

    [DataField]
    public float BigWoundHealthFraction = 0.5f;

    [ViewVariables]
    public bool SleepingForPossession;
}

[Serializable, NetSerializable]
public enum RMCGiantLizardVisualLayers : byte
{
    Base,
    Wounds,
    Tongue,
}

[Serializable, NetSerializable]
public enum RMCGiantLizardVisuals : byte
{
    Body,
    Wounds,
    Tongue,
}

[Serializable, NetSerializable]
public enum RMCGiantLizardBodyVisual : byte
{
    Running,
    Sleeping,
    KnockedDown,
    Dead,
}

[Serializable, NetSerializable]
public enum RMCGiantLizardWoundVisual : byte
{
    None,
    Small,
    Big,
    SmallRest,
    BigRest,
    SmallStun,
    BigStun,
}

public sealed partial class RMCGiantLizardPounceActionEvent : WorldTargetActionEvent;
