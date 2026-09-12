namespace Content.Shared._RMC14.Dropship;

[ByRefEvent]
public readonly record struct DropshipArrivedAtDestinationEvent(Entity<DropshipComponent> Dropship, EntityUid? Destination);
