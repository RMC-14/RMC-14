using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.JoinXeno;

[Serializable, NetSerializable]
public record JoinXenoBurrowedLarvaEvent(NetEntity Hive);
