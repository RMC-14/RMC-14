using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Attachable.Events;

[Serializable, NetSerializable]
public sealed partial class AttachableAttachDoAfterEvent : SimpleDoAfterEvent
{
    public readonly string SlotId;

    public AttachableAttachDoAfterEvent(string slotId)
    {
        SlotId = slotId;
    }
}
