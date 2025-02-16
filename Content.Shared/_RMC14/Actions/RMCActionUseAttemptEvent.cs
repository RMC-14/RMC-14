namespace Content.Shared._RMC14.Actions;

[ByRefEvent]
public record struct RMCActionUseAttemptEvent(EntityUid User, bool Cancelled = false);
