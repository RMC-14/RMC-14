using Robust.Shared.Map;

namespace Content.Shared._RMC14.Construction;

[ByRefEvent]
public record struct RMCConstructionAttemptEvent(
    EntityCoordinates Location,
    string? PrototypeName,
    string? Popup = null,
    bool Cancelled = false
);
