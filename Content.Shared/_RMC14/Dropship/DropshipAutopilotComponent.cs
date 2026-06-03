using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedDropshipSystem))]
public sealed partial class DropshipAutopilotComponent : Component
{
    [DataField, AutoNetworkedField]
    public DropshipAutopilotMode Mode = DropshipAutopilotMode.Disabled;

    [DataField, AutoNetworkedField]
    public EntityUid? RouteHangar;

    [DataField, AutoNetworkedField]
    public EntityUid? LandingZone;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(30);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextDepartureAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? RetryAt;

    [DataField, AutoNetworkedField]
    public DropshipAutopilotStatus Status = DropshipAutopilotStatus.Offline;

    [DataField, AutoNetworkedField]
    public string StatusDetails = string.Empty;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DropshipRemoteControlConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public DropshipRemoteConsoleKind Kind = DropshipRemoteConsoleKind.Shipside;

    [DataField, AutoNetworkedField]
    public EntityUid? LinkedLandingZone;

    [DataField, AutoNetworkedField]
    public bool AllowQuickSummon;
}

public enum DropshipAutopilotMode
{
    Disabled,
    Cycle,
    RecallOnly,
}

public enum DropshipAutopilotStatus
{
    Offline,
    Ready,
    Waiting,
    InFlight,
    Blocked,
    Error,
}

public enum DropshipLaunchSource
{
    RoundInit,
    ManualNavigation,
    RemoteNavigation,
    PlanetsideTerminal,
    Hijack,
    Autopilot,
}

[Serializable, NetSerializable]
public enum DropshipRemoteConsoleKind
{
    Shipside,
    Planetside,
}
