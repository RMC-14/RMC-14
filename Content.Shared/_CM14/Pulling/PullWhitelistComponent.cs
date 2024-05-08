using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Pulling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMPullingSystem))]
public sealed partial class PullWhitelistComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntityWhitelist Whitelist = new();
}
