using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.HiveTeam;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class HiveTeamsComponent : Component
{
    public const int TeamCount = 4;
    public static readonly LocId[] RoleNames =
    [
        "rmc-xeno-ui-hive-team-role-maim",
        "rmc-xeno-ui-hive-team-role-rip",
        "rmc-xeno-ui-hive-team-role-tear",
        "rmc-xeno-ui-hive-team-role-follow-queen",
        "rmc-xeno-ui-hive-team-role-build",
        "rmc-xeno-ui-hive-team-role-backline",
        "rmc-xeno-ui-hive-team-role-capture",
        "rmc-xeno-ui-hive-team-role-stall",
    ];

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
