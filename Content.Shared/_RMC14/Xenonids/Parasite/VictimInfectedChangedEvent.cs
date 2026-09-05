namespace Content.Shared._RMC14.Xenonids.Parasite;

/// <summary>
/// Raised when an entity becomes infected or has its infection removed.
/// </summary>
[ByRefEvent]
public readonly record struct VictimInfectedChangedEvent(EntityUid Target);
