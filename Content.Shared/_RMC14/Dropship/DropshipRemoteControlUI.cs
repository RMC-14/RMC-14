using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship;

[Serializable, NetSerializable]
public enum DropshipRemoteControlUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class DropshipRemoteControlBuiState(
    DropshipRemoteConsoleKind kind,
    NetEntity? linkedLandingZone,
    string linkedLandingZoneName,
    List<DropshipRemoteControlDropshipEntry> dropships,
    List<DropshipRemoteControlDestinationEntry> destinations,
    List<DropshipRemoteControlDestinationEntry> hangars,
    List<DropshipRemoteControlDestinationEntry> landingZones,
    int defaultDelaySeconds,
    int minDelaySeconds,
    int maxDelaySeconds) : BoundUserInterfaceState
{
    public readonly DropshipRemoteConsoleKind Kind = kind;
    public readonly NetEntity? LinkedLandingZone = linkedLandingZone;
    public readonly string LinkedLandingZoneName = linkedLandingZoneName;
    public readonly List<DropshipRemoteControlDropshipEntry> Dropships = dropships;
    public readonly List<DropshipRemoteControlDestinationEntry> Destinations = destinations;
    public readonly List<DropshipRemoteControlDestinationEntry> Hangars = hangars;
    public readonly List<DropshipRemoteControlDestinationEntry> LandingZones = landingZones;
    public readonly int DefaultDelaySeconds = defaultDelaySeconds;
    public readonly int MinDelaySeconds = minDelaySeconds;
    public readonly int MaxDelaySeconds = maxDelaySeconds;
}

[Serializable, NetSerializable]
public readonly record struct DropshipRemoteControlDropshipEntry(
    NetEntity Computer,
    NetEntity Dropship,
    string Name,
    string Location,
    NetEntity? RouteHangar,
    string RouteHangarName,
    NetEntity? LandingZone,
    DropshipAutopilotMode Mode,
    int DelaySeconds,
    int? DepartInSeconds,
    DropshipAutopilotStatus Status,
    string StatusDetails,
    bool InFlight,
    bool Crashed);

[Serializable, NetSerializable]
public readonly record struct DropshipRemoteControlDestinationEntry(
    NetEntity Id,
    string Name,
    NetEntity? OccupiedBy,
    bool Primary,
    List<NetEntity> AvailableDropships);

[Serializable, NetSerializable]
public readonly record struct DropshipNavigationAutopilotStatus(
    DropshipAutopilotMode Mode,
    DropshipAutopilotStatus Status,
    string StatusDetails,
    int? DepartInSeconds);

[Serializable, NetSerializable]
public sealed class DropshipRemoteAutopilotConfigureMsg(
    NetEntity computer,
    DropshipAutopilotMode mode,
    NetEntity? routeHangar,
    NetEntity? landingZone,
    int delaySeconds) : BoundUserInterfaceMessage
{
    public readonly NetEntity Computer = computer;
    public readonly DropshipAutopilotMode Mode = mode;
    public readonly NetEntity? RouteHangar = routeHangar;
    public readonly NetEntity? LandingZone = landingZone;
    public readonly int DelaySeconds = delaySeconds;
}

[Serializable, NetSerializable]
public sealed class DropshipRemoteAutopilotDisableMsg(NetEntity computer) : BoundUserInterfaceMessage
{
    public readonly NetEntity Computer = computer;
}

[Serializable, NetSerializable]
public sealed class DropshipRemoteAutopilotLaunchNowMsg(NetEntity computer) : BoundUserInterfaceMessage
{
    public readonly NetEntity Computer = computer;
}

[Serializable, NetSerializable]
public sealed class DropshipRemoteAutopilotRecallNowMsg(NetEntity computer) : BoundUserInterfaceMessage
{
    public readonly NetEntity Computer = computer;
}

[Serializable, NetSerializable]
public sealed class DropshipRemoteLaunchMsg(NetEntity computer, NetEntity destination) : BoundUserInterfaceMessage
{
    public readonly NetEntity Computer = computer;
    public readonly NetEntity Destination = destination;
}

[Serializable, NetSerializable]
public sealed class DropshipNavigationAutopilotDisableMsg : BoundUserInterfaceMessage;
