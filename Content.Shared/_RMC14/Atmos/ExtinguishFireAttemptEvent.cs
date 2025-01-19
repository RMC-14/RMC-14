namespace Content.Shared._RMC14.Atmos;

[ByRefEvent]
public record struct ExtinguishFireAttemptEvent(EntityUid Extinguisher, EntityUid Target, bool Cancelled = false);
