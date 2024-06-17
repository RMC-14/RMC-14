

namespace Content.Shared._CM14.Attachable;

public sealed partial class AttachableHolderAttachablesAlteredEvent : EntityEventArgs
{
    public readonly EntityUid AttachableUid;
    public readonly string SlotID;
    public readonly bool Attached;
    
    
    public AttachableHolderAttachablesAlteredEvent(EntityUid attachableUid, string slotID, bool attached)
    {
        AttachableUid = attachableUid;
        SlotID = slotID;
        Attached = attached;
    }
}
