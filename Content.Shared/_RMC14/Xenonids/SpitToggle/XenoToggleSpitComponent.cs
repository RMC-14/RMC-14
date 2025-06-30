using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.SpitToggle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoToggleSpitComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool UseAcid = false;

    [DataField, AutoNetworkedField]
    public FixedPoint2 NeuroCost = 50;

    [DataField, AutoNetworkedField]
    public FixedPoint2 AcidCost = 25;

    [DataField, AutoNetworkedField]
    public EntProtoId NeuroProto = "XenoQueenNeuroSpitProjectile";

    [DataField, AutoNetworkedField]
    public EntProtoId AcidProto = "XenoChargedSpitProjectile";
}
