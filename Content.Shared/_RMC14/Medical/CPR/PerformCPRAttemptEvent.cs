namespace Content.Shared._RMC14.Medical.CPR;

[ByRefEvent]
public record struct PerformCPRAttemptEvent(EntityUid Target, bool Cancelled = false);
