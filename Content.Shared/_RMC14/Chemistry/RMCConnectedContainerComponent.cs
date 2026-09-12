using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCConnectedContainerComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string ContainerId;
}
