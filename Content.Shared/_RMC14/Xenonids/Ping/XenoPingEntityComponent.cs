using Content.Shared._RMC14.Ping;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using System.Numerics;

namespace Content.Shared._RMC14.Xenonids.Ping;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class XenoPingEntityComponent : Component, RMCPingEntityComponent
{
    [DataField, AutoNetworkedField]
    public string PingType = "XenoPingMove";

    [DataField, AutoNetworkedField]
    public EntityUid Creator;

    [DataField, AutoNetworkedField]
    public EntityUid Hive;

    [DataField, AutoNetworkedField]
    public TimeSpan Lifetime = TimeSpan.FromSeconds(30);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan DeleteAt;

    [DataField, AutoNetworkedField]
    public EntityUid? AttachedTarget;

    [DataField, AutoNetworkedField]
    public EntityCoordinates? LastKnownCoordinates;

    [DataField, AutoNetworkedField]
    public Vector2 WorldPosition;

    [DataField, AutoNetworkedField]
    public bool ShowWaypoint = true;

    [DataField, AutoNetworkedField]
    public Vector2 AttachedOffset = Vector2.Zero;

    string RMCPingEntityComponent.PingType
    {
        get => PingType;
        set => PingType = value;
    }

    EntityUid RMCPingEntityComponent.Creator
    {
        get => Creator;
        set => Creator = value;
    }

    TimeSpan RMCPingEntityComponent.Lifetime
    {
        get => Lifetime;
        set => Lifetime = value;
    }

    TimeSpan RMCPingEntityComponent.DeleteAt
    {
        get => DeleteAt;
        set => DeleteAt = value;
    }

    EntityUid? RMCPingEntityComponent.AttachedTarget
    {
        get => AttachedTarget;
        set => AttachedTarget = value;
    }

    EntityCoordinates? RMCPingEntityComponent.LastKnownCoordinates
    {
        get => LastKnownCoordinates;
        set => LastKnownCoordinates = value;
    }

    Vector2 RMCPingEntityComponent.WorldPosition
    {
        get => WorldPosition;
        set => WorldPosition = value;
    }

    bool RMCPingEntityComponent.ShowWaypoint
    {
        get => ShowWaypoint;
        set => ShowWaypoint = value;
    }

    Vector2 RMCPingEntityComponent.AttachedOffset
    {
        get => AttachedOffset;
        set => AttachedOffset = value;
    }
}
