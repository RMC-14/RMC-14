using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Attachable.Events;

[Serializable, NetSerializable]
public sealed partial class AttachableToggleDoAfterEvent : SimpleDoAfterEvent
{
    public readonly string SlotId;
    public readonly string PopupText;

    public AttachableToggleDoAfterEvent(string slotId, string popupText)
    {
        SlotId = slotId;
        PopupText = popupText;
    }
}
