using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Construction.Events;

[ByRefEvent]
public readonly record struct XenoConstructionChosenEvent(EntProtoId Choice, EntityUid User);
