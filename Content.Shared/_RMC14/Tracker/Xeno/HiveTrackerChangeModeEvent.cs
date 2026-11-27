using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Tracker.Xeno;

[DataRecord]
[Serializable, NetSerializable]
public sealed partial record HiveTrackerChangeModeEvent(ProtoId<TrackerModePrototype> Mode);
