using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.HiveTeam;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class HiveTeamsComponent : Component
{
    public const int TeamCount = 4;
    public static readonly string[] RoleNames = ["Combat: Maim", "Combat: Rip", "Combat: Tear", "Follow Queen", "Build", "Backline", "Capture", "Stall"];

    [DataField, AutoNetworkedField]
    public List<HiveTeamEntry> Teams = [];
}

[Serializable, NetSerializable]
public sealed class HiveTeamEntry
{
    [DataField]
    public NetEntity? Leader;
    [DataField]
    public List<NetEntity> Members = [];
    [DataField]
    public int Role;
}
