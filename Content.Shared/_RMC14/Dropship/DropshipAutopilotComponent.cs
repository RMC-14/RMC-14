using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

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

    /// <summary>
    /// Zero uses the server's default delay when the component initializes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Delay;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextDepartureAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? RetryAt;

    [DataField, AutoNetworkedField]
    public DropshipAutopilotStatus Status = DropshipAutopilotStatus.Offline;

    [DataField, AutoNetworkedField]
    public string StatusDetails = string.Empty;
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
