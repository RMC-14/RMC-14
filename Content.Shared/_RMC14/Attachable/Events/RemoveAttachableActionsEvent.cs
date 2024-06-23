namespace Content.Shared._RMC14.Attachable.Events;

[ByRefEvent]
public readonly record struct RemoveAttachableActionsEvent(EntityUid User);
