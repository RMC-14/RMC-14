using Content.Shared._CM14.Attachable.Components;

namespace Content.Shared._CM14.Attachable.Events;

[ByRefEvent]
public readonly record struct AttachableToggleStartedEvent(
    Entity<AttachableHolderComponent> Holder,
    EntityUid User,
    string SlotId
);
