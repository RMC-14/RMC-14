using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Tracker;

[Serializable, NetSerializable]
public sealed record LeaderTrackerSelectTargetEvent(NetEntity Target, ProtoId<TrackerModePrototype> Mode);
