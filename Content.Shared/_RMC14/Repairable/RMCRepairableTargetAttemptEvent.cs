namespace Content.Shared._RMC14.Repairable;

[ByRefEvent]
public record struct RMCRepairableTargetAttemptEvent(
    EntityUid User,
    EntityUid Target,
    bool Cancelled = false,
    string? Popup = null
);
