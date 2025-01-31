using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines.Dogtags;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]

//Similiar to info tags comp but only stores names (and ghosts? TODO)
public sealed partial class RMCMemorialComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<string> Names = new();
}
