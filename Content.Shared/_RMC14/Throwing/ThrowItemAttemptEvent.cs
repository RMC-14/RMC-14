namespace Content.Shared._RMC14.Throwing;

[ByRefEvent]
public record struct ThrowItemAttemptEvent(EntityUid User, bool Cancelled = false);
