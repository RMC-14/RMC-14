using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Hands;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCHandsSystem))]
public sealed partial class RMCStorageEjectHandComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;
}
