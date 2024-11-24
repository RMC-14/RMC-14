namespace Content.Shared._RMC14.Hands;

[ByRefEvent]
public readonly record struct ItemPickedUpEvent(EntityUid User, EntityUid Item);
