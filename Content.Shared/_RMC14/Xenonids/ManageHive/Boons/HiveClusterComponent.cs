using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.ManageHive.Boons;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(HiveBoonSystem))]
public sealed partial class HiveClusterComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId TowerReplaceWith = "HivePylonXeno";
}
