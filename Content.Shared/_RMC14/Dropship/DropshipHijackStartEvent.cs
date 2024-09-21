namespace Content.Shared._RMC14.Dropship;

[ByRefEvent]
public readonly record struct DropshipHijackStartEvent(EntityUid? Dropship);
