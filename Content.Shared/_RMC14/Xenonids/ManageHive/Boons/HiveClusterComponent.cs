using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.ManageHive.Boons;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(HiveBoonSystem))]
public sealed partial class HiveClusterComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId TowerReplaceWith = "HivePylonXeno";

    [DataField, AutoNetworkedField]
    public TimeSpan FortificationRepairEvery = TimeSpan.FromSeconds(20);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextFortificationRepairAt;
}
