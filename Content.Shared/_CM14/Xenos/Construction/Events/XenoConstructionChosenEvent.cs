using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Xenos.Construction.Events;

[ByRefEvent]
public readonly record struct XenoConstructionChosenEvent(EntProtoId Choice);
