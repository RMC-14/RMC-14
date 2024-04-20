namespace Content.Shared._CM14.Tackle;

[ByRefEvent]
public record struct CMDisarmEvent(EntityUid User, bool Handled = false);
