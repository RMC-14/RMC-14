using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Pulling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCPullingSystem))]
public sealed partial class PullWhitelistComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntityWhitelist Whitelist = new();
}
