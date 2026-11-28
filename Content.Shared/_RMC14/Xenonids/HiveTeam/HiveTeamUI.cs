using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.HiveTeam;

[Serializable, NetSerializable]
public enum HiveTeamUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum HiveLeaderSquadUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum HiveTeamMemberUIKey : byte
{
    Key
}

/// <summary>
/// Represents a xeno for UI display, sent from server to avoid client PVS issues.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct HiveTeamXeno(NetEntity Entity, string Name, EntProtoId? ProtoId);

/// <summary>
/// Server-sent state containing all xenos and team data for the Queen's hive team UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class HiveTeamBuiState(List<HiveTeamXeno> allXenos, List<HiveTeamEntryState> teams) : BoundUserInterfaceState
{
    public readonly List<HiveTeamXeno> AllXenos = allXenos;
    public readonly List<HiveTeamEntryState> Teams = teams;
}

/// <summary>
/// Server-sent state for a single team entry.
/// </summary>
[Serializable, NetSerializable]
public sealed class HiveTeamEntryState(NetEntity? leader, List<NetEntity> members, int role)
{
    public readonly NetEntity? Leader = leader;
    public readonly List<NetEntity> Members = members;
    public readonly int Role = role;
}

/// <summary>
/// Server-sent state for the hive leader's squad UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class HiveLeaderSquadBuiState(
    List<HiveTeamXeno> allXenos,
    List<HiveTeamEntryState> allTeams,
    HiveTeamEntryState? myTeam,
    int teamIndex,
    string roleName) : BoundUserInterfaceState
{
    public readonly List<HiveTeamXeno> AllXenos = allXenos;
    public readonly List<HiveTeamEntryState> AllTeams = allTeams;
    public readonly HiveTeamEntryState? MyTeam = myTeam;
    public readonly int TeamIndex = teamIndex;
    public readonly string RoleName = roleName;
}

/// <summary>
/// Server-sent state for non-leader team members to view their team.
/// </summary>
[Serializable, NetSerializable]
public sealed class HiveTeamMemberBuiState(
    List<HiveTeamXeno> teamMembers,
    HiveTeamXeno? leader,
    int teamNumber,
    string roleName) : BoundUserInterfaceState
{
    public readonly List<HiveTeamXeno> TeamMembers = teamMembers;
    public readonly HiveTeamXeno? Leader = leader;
    public readonly int TeamNumber = teamNumber;
    public readonly string RoleName = roleName;
}

[Serializable, NetSerializable]
public sealed class HiveTeamSetLeaderMsg(int teamIndex, NetEntity xeno) : BoundUserInterfaceMessage
{
    public readonly int TeamIndex = teamIndex;
    public readonly NetEntity Xeno = xeno;
}

[Serializable, NetSerializable]
public sealed class HiveTeamSetRoleMsg(int teamIndex, int role) : BoundUserInterfaceMessage
{
    public readonly int TeamIndex = teamIndex;
    public readonly int Role = role;
}

[Serializable, NetSerializable]
public sealed class HiveLeaderSquadAnnounceMsg(string message) : BoundUserInterfaceMessage
{
    public readonly string Message = message;
}

[Serializable, NetSerializable]
public sealed class HiveTeamRemoveLeaderMsg(int teamIndex) : BoundUserInterfaceMessage
{
    public readonly int TeamIndex = teamIndex;
}

[Serializable, NetSerializable]
public sealed class HiveTeamAddMemberMsg(int teamIndex, NetEntity xeno) : BoundUserInterfaceMessage
{
    public readonly int TeamIndex = teamIndex;
    public readonly NetEntity Xeno = xeno;
}

[Serializable, NetSerializable]
public sealed class HiveLeaderAddMemberMsg(NetEntity xeno) : BoundUserInterfaceMessage
{
    public readonly NetEntity Xeno = xeno;
}

[Serializable, NetSerializable]
public sealed class HiveLeaderRemoveMemberMsg(NetEntity xeno) : BoundUserInterfaceMessage
{
    public readonly NetEntity Xeno = xeno;
}

[Serializable, NetSerializable]
public sealed class HiveTeamRemoveMemberMsg(int teamIndex, NetEntity xeno) : BoundUserInterfaceMessage
{
    public readonly int TeamIndex = teamIndex;
    public readonly NetEntity Xeno = xeno;
}
