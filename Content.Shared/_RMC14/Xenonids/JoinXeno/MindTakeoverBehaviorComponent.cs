using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.JoinXeno;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(JoinXenoSystem))]
public sealed partial class MindTakeoverBehaviorComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool EjectFromLarvaQueues = true;
}
