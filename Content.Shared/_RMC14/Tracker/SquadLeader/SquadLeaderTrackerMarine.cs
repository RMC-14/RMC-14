using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Tracker.SquadLeader;

[Serializable, NetSerializable]
public readonly record struct SquadLeaderTrackerMarine(NetEntity Id, ProtoId<JobPrototype>? Role, string Name, SpriteSpecifier.Rsi? IconOverride);
