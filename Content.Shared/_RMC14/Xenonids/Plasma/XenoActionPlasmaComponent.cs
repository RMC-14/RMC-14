using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Plasma;

/// <summary>
/// This component indicates the associated action costs Plasma
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoPlasmaSystem))]
public sealed partial class XenoActionPlasmaComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Cost;
}
