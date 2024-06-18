

namespace Content.Shared._CM14.Attachable;

public sealed partial class AttachableAlteredEvent : EntityEventArgs
{
    public readonly EntityUid HolderUid;
    public readonly AttachableAlteredType Alteration;
    
    
    public AttachableAlteredEvent(EntityUid holderUid, AttachableAlteredType alteration)
    {
        HolderUid = holderUid;
        Alteration = alteration;
    }
}

public enum AttachableAlteredType : byte
{
    Attached = 1 << 0,
    Detached = 1 << 1,
    Wielded = 1 << 2,
    Unwielded = 1 << 3,
    Activated = 1 << 4,
    Deactivated = 1 << 5
}
