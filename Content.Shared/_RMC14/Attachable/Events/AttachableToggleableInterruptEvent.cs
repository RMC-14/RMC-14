using Content.Shared._RMC14.Attachable.Components;

namespace Content.Shared._RMC14.Attachable.Events;

[ByRefEvent]
public readonly record struct AttachableToggleableInterruptEvent(
    EntityUid User
);
