using Content.Shared._RMC14.Xenonids.Watch;
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
public sealed class HiveTeamBuiState(List<HiveTeamData> teams, List<Xeno> allXenos) : BoundUserInterfaceState
{
    public readonly List<HiveTeamData> Teams = teams;
    public readonly List<Xeno> AllXenos = allXenos;
}

[Serializable, NetSerializable]
public sealed class HiveLeaderSquadBuiState(HiveTeamData team, string roleName, List<Xeno> allXenos) : BoundUserInterfaceState
{
    public readonly HiveTeamData Team = team;
    public readonly string RoleName = roleName;
    public readonly List<Xeno> AllXenos = allXenos;
}

[Serializable, NetSerializable]
public sealed class HiveTeamData(int index, Xeno? leader, List<Xeno> members, int role = 0)
{
    public readonly int Index = index;
    public Xeno? Leader = leader;
    public readonly List<Xeno> Members = members;
    public int Role = role;
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
