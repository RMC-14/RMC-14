using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Tracker.SquadLeader;

[DataRecord]
[Serializable, NetSerializable]
public sealed record SquadLeaderTrackerChangeModeEvent(ProtoId<TrackerModePrototype> Mode);
