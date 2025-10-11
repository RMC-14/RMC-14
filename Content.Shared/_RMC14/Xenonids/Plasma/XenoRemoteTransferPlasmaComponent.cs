using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Plasma;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class XenoRemoteTransferPlasmaComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaPercentage = 0.75;

    [DataField, AutoNetworkedField]
    public EntProtoId RemotePlasmaTransferActionProtoId = "ActionXenoRemotePlasmaTransfer";
}
