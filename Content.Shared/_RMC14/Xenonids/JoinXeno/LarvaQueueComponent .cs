using Content.Shared._RMC14.Xenonids.JoinXeno;
using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.Hive;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoHiveSystem), typeof(SharedJoinXenoSystem))]
public sealed partial class LarvaQueueComponent : Component
{
    [DataField, AutoNetworkedField]
    public Queue<NetUserId> PlayerQueue = new();

    [DataField, AutoNetworkedField]
    public int MaxQueueSize = 100;

    [DataField, AutoNetworkedField]
    public float ProcessInterval = 0.5f;

    [DataField, AutoNetworkedField]
    public TimeSpan LastProcessed = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> PendingLarvae = new();
}
