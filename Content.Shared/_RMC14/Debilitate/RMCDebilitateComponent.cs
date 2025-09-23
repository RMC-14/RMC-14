using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Debilitate;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCDebilitateSystem))]
public sealed partial class RMCDebilitateComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    [DataField, AutoNetworkedField]
    public TimeSpan Knockdown;
}
