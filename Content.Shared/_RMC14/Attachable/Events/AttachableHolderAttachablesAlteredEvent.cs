namespace Content.Shared._RMC14.Attachable.Events;

[ByRefEvent]
public readonly record struct AttachableHolderAttachablesAlteredEvent(
    EntityUid Attachable,
    string SlotId,
    AttachableAlteredType Alteration
);
