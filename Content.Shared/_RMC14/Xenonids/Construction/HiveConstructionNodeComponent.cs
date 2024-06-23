using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoConstructionSystem))]
public sealed partial class HiveConstructionNodeComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 InitialPlasmaCost;

    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 PlasmaCost;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaStored;

    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Spawn;

    [DataField, AutoNetworkedField]
    public bool BlockOtherNodes = true;
}
