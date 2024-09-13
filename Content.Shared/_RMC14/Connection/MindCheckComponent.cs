using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Connection;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class MindCheckComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool ActiveMindOrGhost = false;
}
