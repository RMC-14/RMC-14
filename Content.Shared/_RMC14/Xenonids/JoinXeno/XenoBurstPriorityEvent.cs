using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.JoinXeno;

[ByRefEvent]
public record struct XenoBurstPriorityEvent(NetUserId? BurstVictimUserId, NetUserId? InfectorUserId, NetEntity? Hive, NetEntity? SpawnedLarva = null);
