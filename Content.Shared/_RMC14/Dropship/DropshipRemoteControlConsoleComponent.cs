using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedDropshipSystem))]
public sealed partial class DropshipRemoteControlConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedLandingZone;

    [AutoNetworkedField]
    public bool Planetside;

    [AutoNetworkedField]
    public string LinkedLandingZoneName = string.Empty;

    [AutoNetworkedField]
    public List<DropshipRemoteControlDropshipEntry> Dropships = new();

    [AutoNetworkedField]
    public List<DropshipRemoteControlDestinationEntry> Destinations = new();

    [AutoNetworkedField]
    public List<DropshipRemoteControlDestinationEntry> Hangars = new();

    [AutoNetworkedField]
    public List<DropshipRemoteControlDestinationEntry> LandingZones = new();

    [AutoNetworkedField]
    public int DefaultDelaySeconds;

    [AutoNetworkedField]
    public int MinDelaySeconds;

    [AutoNetworkedField]
    public int MaxDelaySeconds;
}
