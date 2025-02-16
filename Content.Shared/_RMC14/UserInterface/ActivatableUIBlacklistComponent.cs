using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.UserInterface;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCUserInterfaceSystem))]
public sealed partial class ActivatableUIBlacklistComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityWhitelist Blacklist;
}
