namespace Content.Shared._CM14.Medical.CPR;

[ByRefEvent]
public record struct PerformCPRAttemptEvent(EntityUid Target, bool Cancelled = false);
