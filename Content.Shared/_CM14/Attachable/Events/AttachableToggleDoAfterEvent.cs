using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Attachable.Events;

[Serializable, NetSerializable]
public sealed partial class AttachableToggleDoAfterEvent : SimpleDoAfterEvent
{
    public readonly string SlotId;

    public AttachableToggleDoAfterEvent(string slotId)
    {
        SlotId = slotId;
    }
}
