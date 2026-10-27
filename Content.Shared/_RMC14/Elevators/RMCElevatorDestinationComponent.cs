using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Elevators;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCElevatorDestinationComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ElevatorId = "rmc-elevator";

    [DataField, AutoNetworkedField]
    public string OpenDoors = "all";

    [DataField, AutoNetworkedField]
    public string LinkCode = String.Empty;
}
