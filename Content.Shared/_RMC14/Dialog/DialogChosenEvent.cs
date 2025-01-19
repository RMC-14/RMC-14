namespace Content.Shared._RMC14.Dialog;

[ByRefEvent]
public readonly record struct DialogChosenEvent(EntityUid Actor, int Index);
