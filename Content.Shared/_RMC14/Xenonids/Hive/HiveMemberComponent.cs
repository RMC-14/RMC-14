using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Hive;

// TODO RMC14 replace other hive properties with this component
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoHiveSystem))]
public sealed partial class HiveMemberComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Hive;
}
