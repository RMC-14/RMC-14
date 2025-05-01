using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Tracker;

[DataRecord]
[Serializable, NetSerializable]
public sealed record LeaderTrackerSelectTargetEvent(NetEntity Target, ProtoId<JobPrototype>? Role);
