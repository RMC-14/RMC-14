using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Synth;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSynthSystem))]
public sealed partial class UseOnSynthBlockedComponent : Component
{
    [DataField]
    public LocId Popup = "rmc-species-synth-defib-attempt";

    [DataField, AutoNetworkedField]
    public bool Reversed = false;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;
}
