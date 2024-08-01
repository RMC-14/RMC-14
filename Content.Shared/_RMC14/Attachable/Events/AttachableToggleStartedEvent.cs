using Content.Shared._RMC14.Attachable.Components;

namespace Content.Shared._RMC14.Attachable.Events;

[ByRefEvent]
public readonly record struct AttachableToggleStartedEvent(
    EntityUid Holder,
    EntityUid User,
    string SlotId
);
