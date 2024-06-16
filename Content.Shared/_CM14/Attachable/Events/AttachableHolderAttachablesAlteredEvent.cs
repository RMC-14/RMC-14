using Content.Shared.DoAfter;
using Robust.Shared.Serialization;


namespace Content.Shared._CM14.Attachable;

public sealed partial class AttachableHolderAttachablesAlteredEvent : EntityEventArgs
{
    public readonly EntityUid AttachableUid;
    public readonly bool Attached;
    
    
    public AttachableHolderAttachablesAlteredEvent(EntityUid attachableUid, bool attached)
    {
        AttachableUid = attachableUid;
        Attached = attached;
    }
}
