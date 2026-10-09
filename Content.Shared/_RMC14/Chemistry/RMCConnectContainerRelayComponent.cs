using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCConnectContainerRelayComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? ContainerOwner;
}
