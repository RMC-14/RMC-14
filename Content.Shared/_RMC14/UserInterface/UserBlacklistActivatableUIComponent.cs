using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.UserInterface;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCUserInterfaceSystem))]
public sealed partial class UserBlacklistActivatableUIComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<Enum> Keys = new();
}
