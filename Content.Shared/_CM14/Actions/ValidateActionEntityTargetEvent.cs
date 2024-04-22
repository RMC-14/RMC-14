namespace Content.Shared._CM14.Actions;

[ByRefEvent]
public record struct ValidateActionEntityTargetEvent(EntityUid User, EntityUid Target, bool Cancelled = false);
