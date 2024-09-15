using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Connection;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class MindCheckComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool ActiveMindOrGhost = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan CheckEvery = TimeSpan.FromSeconds(30);

    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TimeSpan NextCheck;
}
