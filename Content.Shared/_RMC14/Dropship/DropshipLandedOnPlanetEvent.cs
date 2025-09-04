namespace Content.Shared._RMC14.Dropship;

[ByRefEvent]
public readonly record struct DropshipLandedOnPlanetEvent(Entity<DropshipComponent> Dropship);
