namespace Content.Shared._RMC14.Attachable.Events;

[ByRefEvent]
public readonly record struct AttachableAlteredEvent(
    EntityUid Holder,
    AttachableAlteredType Alteration,
    EntityUid? User = null
);

public enum AttachableAlteredType : byte
{
    Attached = 1 << 0,
    Detached = 1 << 1,
    Wielded = 1 << 2,
    Unwielded = 1 << 3,
    Activated = 1 << 4,
    Deactivated = 1 << 5,
    Interrupted = 1 << 6, // This is used when a toggleable attachment is deactivated by something other than its hotkey or action.
    AppearanceChanged = 1 << 7,
    DetachedDeactivated = Detached | Deactivated,
}
