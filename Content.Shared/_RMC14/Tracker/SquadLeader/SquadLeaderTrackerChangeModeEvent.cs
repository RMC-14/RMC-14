using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Tracker.SquadLeader;

[DataRecord]
[Serializable, NetSerializable]
public sealed record SquadLeaderTrackerChangeModeEvent(SquadLeaderTrackerMode Mode);
