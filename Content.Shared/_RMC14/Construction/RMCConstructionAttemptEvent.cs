using Content.Shared.Construction.Prototypes;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Construction;

[ByRefEvent]
public record struct RMCConstructionAttemptEvent(
    EntityCoordinates Location,
    ConstructionPrototype Prototype,
    string? Popup = null,
    EntityUid? User = null,
    bool Cancelled = false
);
