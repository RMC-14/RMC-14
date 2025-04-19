using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Buckle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCBuckleSystem))]
public sealed partial class BuckleWhitelistComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;
}
