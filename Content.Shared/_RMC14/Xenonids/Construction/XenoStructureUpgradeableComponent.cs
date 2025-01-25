using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoConstructionSystem))]
public sealed partial class XenoStructureUpgradeableComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId? To;

    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 Cost;
}
