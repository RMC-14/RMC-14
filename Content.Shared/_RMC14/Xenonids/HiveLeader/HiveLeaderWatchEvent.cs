using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.HiveLeader;

[Serializable, NetSerializable]
public sealed record HiveLeaderWatchEvent(NetEntity Leader);
