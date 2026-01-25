using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Hands;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCHandsSystem))]
public sealed partial class WhitelistPickupComponent : Component
{
    [DataField]
    public EntityWhitelist Whitelist = new();

    [DataField]
    public bool AllowDead;
}
