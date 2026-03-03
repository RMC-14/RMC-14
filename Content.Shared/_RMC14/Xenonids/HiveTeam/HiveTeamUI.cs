using Content.Shared._RMC14.Xenonids.Watch;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.HiveTeam;

[Serializable, NetSerializable]
public enum HiveTeamUIKey : byte
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
public sealed class HiveTeamData(int index, Xeno? leader, List<Xeno> members)
{
    public readonly int Index = index;
    public Xeno? Leader = leader;
    public readonly List<Xeno> Members = members;
}

[Serializable, NetSerializable]
public sealed class HiveTeamSetLeaderMsg(int teamIndex, NetEntity xeno) : BoundUserInterfaceMessage
{
    public readonly int TeamIndex = teamIndex;
    public readonly NetEntity Xeno = xeno;
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
public sealed class HiveTeamRemoveMemberMsg(int teamIndex, NetEntity xeno) : BoundUserInterfaceMessage
{
    public readonly int TeamIndex = teamIndex;
    public readonly NetEntity Xeno = xeno;
}
