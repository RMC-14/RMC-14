namespace Content.Shared._RMC14.Actions;

[ByRefEvent]
public record struct RMCActionUseAttemptEvent(EntityUid User, EntityUid? Target, bool Cancelled = false);
