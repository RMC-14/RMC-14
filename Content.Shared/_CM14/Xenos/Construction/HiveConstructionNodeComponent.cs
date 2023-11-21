using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Xenos.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoConstructionSystem))]
public sealed partial class HiveConstructionNodeComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 InitialPlasmaCost;

    [DataField(required: true), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 PlasmaCost;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 PlasmaStored;

    [DataField(required: true), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId Spawn;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool BlockOtherNodes = true;
}
