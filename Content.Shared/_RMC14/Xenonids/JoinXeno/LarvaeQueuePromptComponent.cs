using Content.Shared._RMC14.Xenonids.JoinXeno;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Hive;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedJoinXenoSystem))]
public sealed partial class LarvaQueuePromptComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<NetUserId, (NetEntity Larva, TimeSpan ExpiresAt)> PendingPrompts = new();

    [DataField]
    public TimeSpan PromptTimeout = TimeSpan.FromSeconds(15);
}
