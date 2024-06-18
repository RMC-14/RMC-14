using Content.Shared.DoAfter;
using Robust.Shared.Serialization;


namespace Content.Shared._CM14.Attachable;

[Serializable, NetSerializable]
public sealed partial class AttachableToggleDoAfterEvent : SimpleDoAfterEvent
{
    public readonly string SlotID;
    
    public AttachableToggleDoAfterEvent(string slotID)
    {
        SlotID = slotID;
    }
}
