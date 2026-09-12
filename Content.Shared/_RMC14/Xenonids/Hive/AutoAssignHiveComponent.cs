using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Hive;

/// <summary>
/// Given a hive slot, attempts to auto assign itself to said hive
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoHiveSystem))]
public sealed partial class AutoAssignHiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Hive = HiveSlots.Normal;
}
