namespace Content.Shared._RMC14.Xenonids.HiveLeader;

[ByRefEvent]
public readonly record struct HiveLeaderStatusChangedEvent(bool BecameLeader);
