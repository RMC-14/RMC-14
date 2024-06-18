

namespace Content.Shared._CM14.Attachable;

public sealed partial class AttachableToggleStartedEvent : EntityEventArgs
{
    public readonly Entity<AttachableHolderComponent> Holder;
    public readonly EntityUid UserUid;
    public readonly string SlotID;
    
    
    public AttachableToggleStartedEvent(Entity<AttachableHolderComponent> holder, EntityUid userUid, string slotID)
    {
        Holder = holder;
        UserUid = userUid;
        SlotID = slotID;
    }
}
