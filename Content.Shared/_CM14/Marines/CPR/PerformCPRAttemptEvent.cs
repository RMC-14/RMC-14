namespace Content.Shared._CM14.Marines.CPR;

[ByRefEvent]
public record struct PerformCPRAttemptEvent(EntityUid Target, bool Cancelled = false);
