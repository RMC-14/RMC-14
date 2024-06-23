namespace Content.Shared._RMC14.Light;

[ByRefEvent]
public record struct LightBurnHandAttemptEvent(EntityUid User, EntityUid Light, bool Cancelled = false);
