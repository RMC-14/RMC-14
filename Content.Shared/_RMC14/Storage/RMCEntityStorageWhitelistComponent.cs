using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Storage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCStorageSystem))]
public sealed partial class RMCEntityStorageWhitelistComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;
}
