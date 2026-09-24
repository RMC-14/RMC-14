using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Pointing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCPointingArrowComponent : Component
{
    [DataField, AutoNetworkedField]
    public NetEntity? Source;
}
