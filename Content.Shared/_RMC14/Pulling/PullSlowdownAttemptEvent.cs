namespace Content.Shared._RMC14.Pulling;

[ByRefEvent]
public record struct PullSlowdownAttemptEvent(EntityUid Target, bool Cancelled = false);
