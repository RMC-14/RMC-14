using Content.Shared._RMC14.Ping;
using Content.Shared._RMC14.Xenonids.Hive;

namespace Content.Shared._RMC14.Xenonids.Ping;

public abstract class SharedXenoPingSystem : SharedRMCPingSystem<XenoPingEntityComponent, XenoPingDataComponent>
{
    [Dependency] protected readonly SharedXenoHiveSystem _hive = default!;
}
