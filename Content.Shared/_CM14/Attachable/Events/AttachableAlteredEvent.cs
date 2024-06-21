namespace Content.Shared._CM14.Attachable.Events;

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
    DetachedDeactivated = Detached | Deactivated,
}
