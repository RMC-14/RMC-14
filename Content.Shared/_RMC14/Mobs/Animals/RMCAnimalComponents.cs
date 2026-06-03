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
    public float RestHealFraction = 0.05f;

    [DataField]
    public float AiFeedHealFraction = 0.15f;

    [DataField]
    public float AiFeedRange = 1.5f;

    [DataField]
    public float FoodSearchRange = 6f;

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

[RegisterComponent]
public sealed partial class RMCBatHangingComponent : Component
{
    [DataField]
    public bool Hanging;

    [DataField]
    public TimeSpan CheckCooldown = TimeSpan.FromSeconds(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextCheckAt;

    [DataField]
    public float HangChance = 0.25f;

    [DataField]
    public float WakeChance = 0.08f;

    [DataField]
    public float DisturbanceWakeChance = 0.35f;

    [DataField]
    public float DisturbanceRange = 4f;

    [DataField]
    public bool RequireBlockedNorth = true;
}

[Serializable, NetSerializable]
public enum RMCBatVisuals : byte
{
    Hanging,
}

[RegisterComponent]
public sealed partial class RMCSpiderNestMemberComponent : Component
{
    [DataField]
    public float IdleSkitterChance = 0.01f;

    [DataField]
    public TimeSpan IdleSkitterCooldown = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan IdleSkitterDuration = TimeSpan.FromSeconds(5);

    [DataField]
    public float IdleSkitterSpeed = 5f;

    [ViewVariables]
    public TimeSpan NextIdleSkitterAt;

    [ViewVariables]
    public TimeSpan IdleSkitterUntil;

    [ViewVariables]
    public bool IdleSkittering;
}

[RegisterComponent]
public sealed partial class RMCSpiderVenomComponent : Component
{
    [DataField]
    public float PrickPopupChance = 0.05f;

    [DataField]
    public TimeSpan DazeTime = TimeSpan.Zero;
}

[RegisterComponent]
public sealed partial class RMCSpiderNurseComponent : Component
{
    [DataField]
    public EntProtoId EggPrototype = "RMCSpiderEgg";

    [DataField]
    public EntProtoId CocoonPrototype = "RMCSpiderCocoon";

    [DataField]
    public EntProtoId LargeCocoonPrototype = "RMCSpiderCocoonLarge";

    [DataField]
    public EntProtoId WebPrototype = "RMCSpiderWeb";

    [DataField]
    public float NestRange = 10f;

    [DataField]
    public int MaxActiveSpiders = 20;

    [DataField]
    public int MaxEggs = 6;

    [DataField]
    public int MaxWebs = 24;

    [DataField]
    public int MaxCocoons = 8;

    [DataField]
    public float CocoonRange = 1.5f;

    [DataField]
    public float TargetSearchRange = 10f;

    [DataField]
    public TimeSpan TargetGiveUpTime = TimeSpan.FromSeconds(10);

    [DataField]
    public float MoveToTargetSpeed = 3f;

    [DataField]
    public TimeSpan WebSpinTime = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan EggLayTime = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan CocoonSpinTime = TimeSpan.FromSeconds(5);

    [DataField]
    public float IdleWorkChance = 0.30f;

    [DataField]
    public TimeSpan ThinkCooldown = TimeSpan.FromSeconds(3);

    [ViewVariables]
    public TimeSpan NextThinkAt;

    [ViewVariables]
    public TimeSpan BusyUntil;

    [ViewVariables]
    public RMCSpiderNurseWork BusyWork = RMCSpiderNurseWork.None;

    [ViewVariables]
    public EntityUid? WorkTarget;

    [ViewVariables]
    public TimeSpan TargetAcquiredAt;

    [ViewVariables]
    public int Fed;
}

[Serializable, NetSerializable]
public enum RMCSpiderNurseWork : byte
{
    None,
    MovingToTarget,
    SpinWeb,
    LayEggs,
    Cocoon,
}

[RegisterComponent]
public sealed partial class RMCSpiderWebComponent : Component
{
    [DataField]
    public float ProjectileBlockChance = 0.30f;

    [DataField]
    public float MobRootChance = 0.50f;

    [DataField]
    public TimeSpan MobRootTime = TimeSpan.FromSeconds(1.5);
}

[RegisterComponent]
public sealed partial class RMCSpiderEggComponent : Component
{
    [DataField]
    public EntProtoId SpawnPrototype = "RMCMobSpiderling";

    [DataField]
    public float NestRange = 10f;

    [DataField]
    public int MaxActiveSpiders = 20;

    [DataField]
    public int MinSpawned = 6;

    [DataField]
    public int MaxSpawned = 24;

    [DataField]
    public TimeSpan HatchMin = TimeSpan.FromSeconds(90);

    [DataField]
    public TimeSpan HatchMax = TimeSpan.FromSeconds(180);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan HatchAt;
}

[RegisterComponent]
public sealed partial class RMCSpiderlingGrowthComponent : Component
{
    [DataField]
    public TimeSpan GrowMin = TimeSpan.FromSeconds(90);

    [DataField]
    public TimeSpan GrowMax = TimeSpan.FromSeconds(180);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan GrowAt;

    [DataField]
    public EntProtoId GuardPrototype = "RMCMobSpiderGuard";

    [DataField]
    public EntProtoId HunterPrototype = "RMCMobSpiderHunter";

    [DataField]
    public EntProtoId NursePrototype = "RMCMobSpiderNurse";

    [DataField]
    public EntProtoId RemainsPrototype = "RMCSpiderlingRemains";

    [DataField]
    public float GuardWeight = 60f;

    [DataField]
    public float HunterWeight = 30f;

    [DataField]
    public float NurseWeight = 10f;

    [DataField]
    public float GrowChance = 0.50f;

    [DataField]
    public bool NoGrow;

    [DataField]
    public TimeSpan SkitterCooldown = TimeSpan.FromSeconds(3);

    [ViewVariables]
    public TimeSpan NextSkitterAt;

    [DataField]
    public float SkitterChance = 0.25f;

    [DataField]
    public float ChitterChance = 0.01f;

    [DataField]
    public float SkitterRange = 5f;

    [DataField]
    public float VentSearchChance = 0.05f;

    [DataField]
    public float VentSearchRange = 7f;

    [DataField]
    public float VentMoveSpeed = 5f;

    [ViewVariables]
    public bool SpawnedRemains;
}

[RegisterComponent]
public sealed partial class RMCSpiderCocoonComponent : Component
{
    public const string DefaultContainerId = "rmc-spider-cocoon";

    [DataField]
    public string ContainerId = DefaultContainerId;

    [DataField]
    public int MaxContents = 24;
}

[RegisterComponent]
public sealed partial class RMCTinyLizardComponent : Component
{
    [DataField]
    public float HissChance = 0.35f;

    [DataField]
    public SoundSpecifier HissSound = new SoundPathSpecifier("/Audio/Animals/snake_hiss.ogg");

    [DataField]
    public float ShooKnockback = 0.75f;

    [DataField]
    public float ShooKnockbackSpeed = 6f;

    [DataField]
    public TimeSpan StompPopupCooldown = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public TimeSpan NextStompPopupAt;
}

[RegisterComponent]
public sealed partial class RMCAnimalPreyComponent : Component
{
}

[RegisterComponent]
public sealed partial class RMCRodentBehaviorComponent : Component
{
    [DataField]
    public TimeSpan ThinkCooldown = TimeSpan.FromSeconds(3);

    [DataField]
    public float SleepChance = 0.005f;

    [DataField]
    public float WakeChance = 0.01f;

    [DataField]
    public TimeSpan SleepDurationMin = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan SleepDurationMax = TimeSpan.FromSeconds(60);

    [DataField]
    public float SnuffleChance = 0.05f;

    [DataField]
    public TimeSpan SnuffleCooldown = TimeSpan.FromSeconds(5);

    [DataField]
    public float SqueakOnCollideChance = 0.05f;

    [DataField]
    public TimeSpan SqueakCooldown = TimeSpan.FromSeconds(3);

    [DataField]
    public SoundSpecifier SqueakSound = new SoundPathSpecifier("/Audio/Animals/mouse_squeak.ogg", AudioParams.Default.WithVolume(-3));

    [ViewVariables]
    public TimeSpan NextThinkAt;

    [ViewVariables]
    public TimeSpan SleepUntil;

    [ViewVariables]
    public TimeSpan NextSnuffleAt;

    [ViewVariables]
    public TimeSpan NextSqueakAt;

    [ViewVariables]
    public bool Sleeping;
}

[RegisterComponent]
public sealed partial class RMCCatHunterComponent : Component
{
    [DataField]
    public float SearchRange = 7f;

    [DataField]
    public float AttackRange = 1.2f;

    [DataField]
    public TimeSpan ThinkCooldown = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public TimeSpan NextThinkAt;

    [DataField]
    public float MoveSpeed = 3f;

    [DataField]
    public int MaxPlayAttacks = 5;

    [ViewVariables]
    public int PlayCounter;

    [ViewVariables]
    public EntityUid? MovementTarget;

    [DataField]
    public TimeSpan PlayBreakCooldown = TimeSpan.FromSeconds(4);

    [DataField]
    public float ThreatenRange = 3f;

    [DataField]
    public float ThreatenChance = 0.15f;

    [DataField]
    public TimeSpan ThreatenCooldown = TimeSpan.FromSeconds(3);

    [ViewVariables]
    public TimeSpan NextThreatenAt;

    [DataField]
    public TimeSpan MeowCooldownMin = TimeSpan.FromSeconds(40);

    [DataField]
    public TimeSpan MeowCooldownMax = TimeSpan.FromSeconds(60);

    [ViewVariables]
    public TimeSpan NextMeowAt;

    [DataField]
    public SoundSpecifier MeowSound = new SoundPathSpecifier("/Audio/Animals/cat_meow.ogg", AudioParams.Default.WithVolume(-4));

    [DataField]
    public SoundSpecifier HuntHitSound = new SoundCollectionSpecifier("AlienClaw", AudioParams.Default.WithVolume(-5));

    [DataField]
    public TimeSpan PlayerPreyKnockdown = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan PlayerPreySlowdown = TimeSpan.FromSeconds(4);

    [DataField]
    public DamageSpecifier PlayerPreyDamage = new()
    {
        DamageDict = { { "Blunt", FixedPoint2.New(25) } }
    };

    [DataField]
    public DamageSpecifier NpcPreyDamage = new()
    {
        DamageDict = { { "Blunt", FixedPoint2.New(200) } }
    };

    [DataField]
    public EntityWhitelist? PreyWhitelist;
}

[RegisterComponent]
public sealed partial class RMCParrotComponent : Component
{
    public const string DefaultContainerId = "rmc-parrot-held-item";

    [DataField]
    public string ContainerId = DefaultContainerId;

    [DataField]
    public float SearchRange = 5f;

    [DataField]
    public float PickupRange = 1.2f;

    [DataField]
    public float PerchRange = 7f;

    [DataField]
    public float PerchArriveRange = 1f;

    [DataField]
    public float FlySpeed = 5f;

    [DataField]
    public float FleeSpeed = 7f;

    [DataField]
    public float PanicFlySpeed = 8f;

    [DataField]
    public float AttackFlySpeed = 6f;

    [DataField]
    public float AttackRange = 1.2f;

    [DataField]
    public float AttackDamageMin = 5f;

    [DataField]
    public float AttackDamageMax = 10f;

    [DataField]
    public float WeakTargetDamageFraction = 0.5f;

    [DataField]
    public float StealChance = 0.20f;

    [DataField]
    public float WakeChance = 0.08f;

    [DataField]
    public TimeSpan ThinkCooldown = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan FleeDuration = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan AttackDuration = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan ShotPanicDuration = TimeSpan.FromSeconds(12);

    [DataField]
    public TimeSpan AttackCooldown = TimeSpan.FromSeconds(1.5);

    [DataField]
    public TimeSpan PanicMoveCooldownMin = TimeSpan.FromSeconds(0.35);

    [DataField]
    public TimeSpan PanicMoveCooldownMax = TimeSpan.FromSeconds(0.9);

    [ViewVariables]
    public TimeSpan NextThinkAt;

    [ViewVariables]
    public TimeSpan NextAttackAt;

    [ViewVariables]
    public TimeSpan NextPanicMoveAt;

    [DataField]
    public HashSet<ProtoId<ItemSizePrototype>> StolenSizes = new()
    {
        "Tiny",
        "Small",
    };

    [DataField]
    public EntityWhitelist? PerchWhitelist;

    [ViewVariables]
    public EntityUid? HeldItem;

    [ViewVariables]
    public EntityUid? Perch;

    [ViewVariables]
    public bool Perched;

    [ViewVariables]
    public RMCParrotBehavior Behavior;

    [ViewVariables]
    public EntityUid? BehaviorTarget;

    [ViewVariables]
    public TimeSpan BehaviorUntil;
}

[Serializable, NetSerializable]
public enum RMCParrotVisuals : byte
{
    Perched,
}

public enum RMCParrotBehavior : byte
{
    None,
    Panic,
    Flee,
    Attack,
}

[RegisterComponent]
public sealed partial class RMCCowTippableComponent : Component
{
    [DataField]
    public TimeSpan TipTime = TimeSpan.FromSeconds(30);

    [DataField]
    public TimeSpan TipCooldown = TimeSpan.FromSeconds(10);

    [ViewVariables]
    public TimeSpan NextTipAt;

    [ViewVariables]
    public TimeSpan TippedUntil;
}

[RegisterComponent]
public sealed partial class RMCGoatTemperComponent : Component
{
    [DataField]
    public float SearchRange = 6f;

    [DataField]
    public float MadChance = 0.01f;

    [DataField]
    public float CalmChance = 0.10f;

    [DataField]
    public TimeSpan ThinkCooldown = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public TimeSpan NextThinkAt;
}

[RegisterComponent]
public sealed partial class RMCChickenComponent : Component
{
}

[RegisterComponent]
public sealed partial class RMCChickenFedEggLayerComponent : Component
{
    [DataField]
    public EntProtoId EggPrototype = "FoodEgg";

    [DataField]
    public EntProtoId FertilizedEggPrototype = "RMCFoodEggChickenFertilized";

    [DataField]
    public float FertilizedEggChance = 0.10f;

    [DataField]
    public int MaxNearbyChickens = 50;

    [DataField]
    public float ChickenCapRange = 64f;

    [DataField]
    public int MaxEggCredits = 8;

    [DataField]
    public int MinFeedCredits = 1;

    [DataField]
    public int MaxFeedCredits = 4;

    [DataField]
    public TimeSpan LayCheckCooldown = TimeSpan.FromSeconds(15);

    [DataField]
    public float LayChance = 0.45f;

    [DataField]
    public string FeedTag = "Wheat";

    [ViewVariables]
    public int EggCredits;

    [ViewVariables]
    public TimeSpan NextLayCheckAt;
}

[RegisterComponent]
public sealed partial class RMCChickenEggHatchComponent : Component
{
    [DataField]
    public EntProtoId SpawnPrototype = "RMCMobChick";

    [DataField]
    public TimeSpan HatchMin = TimeSpan.FromSeconds(90);

    [DataField]
    public TimeSpan HatchMax = TimeSpan.FromSeconds(180);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan HatchAt;
}

[RegisterComponent]
public sealed partial class RMCChickGrowthComponent : Component
{
    [DataField]
    public List<EntProtoId> MaturePrototypes = new()
    {
        "RMCMobChicken",
        "RMCMobChicken1",
        "RMCMobChicken2",
    };

    [DataField]
    public TimeSpan GrowMin = TimeSpan.FromSeconds(90);

    [DataField]
    public TimeSpan GrowMax = TimeSpan.FromSeconds(180);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan GrowAt;
}

[RegisterComponent]
public sealed partial class RMCAnimalSpawnerComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Prototype = default;

    [DataField]
    public float InitialChance = 0.66f;

    [DataField]
    public int MaxAlive = 4;

    [DataField]
    public TimeSpan LateSpawnMin = TimeSpan.FromMinutes(35);

    [DataField]
    public TimeSpan LateSpawnMax = TimeSpan.FromMinutes(50);

    [DataField]
    public TimeSpan RetryMin = TimeSpan.FromMinutes(15);

    [DataField]
    public TimeSpan RetryMax = TimeSpan.FromMinutes(25);

    [DataField]
    public float WitnessRange = 7f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextLateSpawnAt;

    [ViewVariables]
    public List<EntityUid> Spawned = new();
}

public sealed partial class RMCGiantLizardPounceActionEvent : WorldTargetActionEvent;
