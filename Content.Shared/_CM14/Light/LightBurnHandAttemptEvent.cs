namespace Content.Shared._CM14.Light;

[ByRefEvent]
public record struct LightBurnHandAttemptEvent(EntityUid User, EntityUid Light, bool Cancelled = false);
