using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.IdentityManagement;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FixedIdentityComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public LocId? Name;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;
}
