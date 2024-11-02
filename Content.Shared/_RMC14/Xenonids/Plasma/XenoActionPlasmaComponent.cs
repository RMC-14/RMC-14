using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Plasma;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoPlasmaSystem))]
public sealed partial class XenoActionPlasmaComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Cost;
}
