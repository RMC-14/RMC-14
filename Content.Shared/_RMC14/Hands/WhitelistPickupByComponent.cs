using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Hands;

[RegisterComponent, NetworkedComponent]
public sealed partial class WhitelistPickupByComponent : Component
{
    [DataField]
    public EntityWhitelist Whitelist = new();
}
