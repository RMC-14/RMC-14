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

    [ViewVariables]
    public bool SleepingForPossession;
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
    public TimeSpan GrowStepCooldown = TimeSpan.FromSeconds(1.5);

    [DataField]
    public float GrowProgressRequired = 100f;

    [DataField]
    public float GrowProgressMin = 0f;

    [DataField]
    public float GrowProgressMax = 2f;

    [ViewVariables]
    public float GrowProgress = -1f;

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
