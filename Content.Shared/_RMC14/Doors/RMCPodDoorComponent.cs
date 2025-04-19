using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Doors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMDoorSystem))]
public sealed partial class RMCPodDoorComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? Id;
}
