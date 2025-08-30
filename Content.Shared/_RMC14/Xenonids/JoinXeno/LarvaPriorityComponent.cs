using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.JoinXeno;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedJoinXenoSystem))]
public sealed partial class LarvaPriorityComponent : Component
{
    [DataField, AutoNetworkedField]
    public NetUserId? OriginalParasiteUserId;

    [DataField, AutoNetworkedField]
    public NetUserId? BurstVictimUserId;

    [DataField, AutoNetworkedField]
    public bool ParasiteOffered = false;

    [DataField, AutoNetworkedField]
    public bool VictimOffered = false;

    [DataField, AutoNetworkedField]
    public bool HasPriorityPlayers = false;
}
