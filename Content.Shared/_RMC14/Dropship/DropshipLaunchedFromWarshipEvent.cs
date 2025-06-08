namespace Content.Shared._RMC14.Dropship;

[ByRefEvent]
public readonly record struct DropshipLaunchedFromWarshipEvent(Entity<DropshipComponent> Dropship);
