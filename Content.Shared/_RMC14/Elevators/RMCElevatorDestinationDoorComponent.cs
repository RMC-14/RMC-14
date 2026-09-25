using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Elevators;

/// <summary>
/// Controls doors that should close when an elevator is not at their linked estination
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCElevatorDestinationDoorComponent : Component
{

    [DataField, AutoNetworkedField]
    public string ElevatorId = "rmc-elevator";

    [DataField, AutoNetworkedField]
    public EntityUid? LinkedDestination;

    [DataField, AutoNetworkedField]
    public string LinkCode = String.Empty;
}
