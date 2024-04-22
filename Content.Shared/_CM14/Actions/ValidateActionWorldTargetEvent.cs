using Robust.Shared.Map;

namespace Content.Shared._CM14.Actions;

[ByRefEvent]
public record struct ValidateActionWorldTargetEvent(EntityUid User, EntityCoordinates Target, bool Cancelled = false);
