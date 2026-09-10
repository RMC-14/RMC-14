using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Elevators;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCElevatorDoorComponent : Component
{
    /// <summary>
    /// Forces the door to open at destinations with a matching OpenDoors string.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> OpenAtIds = new();
}
