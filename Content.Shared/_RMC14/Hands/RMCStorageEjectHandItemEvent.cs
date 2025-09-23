namespace Content.Shared._RMC14.Hands;

[ByRefEvent]
public record struct RMCStorageEjectHandItemEvent(EntityUid User, bool Handled = false);
