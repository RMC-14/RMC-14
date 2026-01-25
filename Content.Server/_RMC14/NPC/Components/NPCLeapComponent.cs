using Content.Shared.Actions.Components;
using Content.Shared.DoAfter;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.NPC.Components;

[RegisterComponent]
public sealed partial class NPCLeapComponent : Component
{
    [ViewVariables]
    public LeapStatus Status = LeapStatus.Normal;

    [ViewVariables]
    public EntityUid Target;

    [DataField]
    public EntProtoId<WorldTargetActionComponent> ActionId = "ActionXenoLeap";

    [DataField]
    public DoAfterId? CurrentDoAfter;

    [ViewVariables]
    public EntityCoordinates Destination;

    [ViewVariables(VVAccess.ReadWrite)]
    public float LeapDistance = 3.5f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float MaxAngleDegrees = 5;

    [ViewVariables(VVAccess.ReadWrite)]
    public CollisionGroup Mask = CollisionGroup.SmallMobMask;
}

public enum LeapStatus : byte
{
    NotInSight,

    Unspecified,

    TargetBadAngle,

    TargetOutOfRange,

    TargetUnreachable,

    Normal,

    Finished,
}
