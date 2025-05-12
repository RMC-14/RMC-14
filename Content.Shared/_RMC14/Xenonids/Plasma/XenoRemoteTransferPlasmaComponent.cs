using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Plasma;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class XenoRemoteTransferPlasmaComponent : Component
{
    [DataField("Percentage"), AutoNetworkedField]
    public FixedPoint2 PlasmaPercentage = 0.75;
}
