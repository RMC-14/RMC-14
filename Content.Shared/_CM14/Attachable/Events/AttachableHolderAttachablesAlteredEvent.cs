

namespace Content.Shared._CM14.Attachable;

public sealed partial class AttachableHolderAttachablesAlteredEvent : EntityEventArgs
{
    public readonly EntityUid AttachableUid;
    public readonly string SlotID;
    public readonly AttachableAlteredType Alteration;
    
    
    public AttachableHolderAttachablesAlteredEvent(EntityUid attachableUid, string slotID, AttachableAlteredType alteration)
    {
        AttachableUid = attachableUid;
        SlotID = slotID;
        Alteration = alteration;
    }
}
