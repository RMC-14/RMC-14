using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Storage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IgnoreContentsSizeComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntityWhitelist Items = new();
}
