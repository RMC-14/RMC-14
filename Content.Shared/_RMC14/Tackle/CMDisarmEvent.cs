namespace Content.Shared._RMC14.Tackle;

[ByRefEvent]
public record struct CMDisarmEvent(EntityUid User, bool Handled = false);
