using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Map;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCMapSystem))]
public sealed partial class RMCDeleteAnchoredOnInitComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;
}
