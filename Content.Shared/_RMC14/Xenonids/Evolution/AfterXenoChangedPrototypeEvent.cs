using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Evolution;

[ByRefEvent]
public readonly record struct AfterXenoChangedPrototypeEvent(EntityUid Xeno, EntProtoId? OldProtoId);
