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
