namespace Content.Shared._RMC14.Xenonids;

/// <summary>
/// Raised when an entity gains or loses its xeno component.
/// </summary>
[ByRefEvent]
public readonly record struct XenoComponentChangedEvent(EntityUid Target);
