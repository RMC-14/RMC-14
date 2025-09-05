using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Plasma;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class XenoRemoteTransferPlasmaComponent : Component
{
    [DataField("Percentage"), AutoNetworkedField]
    public FixedPoint2 PlasmaPercentage = 0.75;

    public EntProtoId RemotePlasmaTransferActionProtoId = "ActionXenoRemotePlasmaTransfer";

    public EntityUid? RemotePlasmaTransferAction;

    public ActionsContainerComponent? ActionsContainer;
}
