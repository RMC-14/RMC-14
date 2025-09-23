using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.PowerLoader.Events;

[Serializable, NetSerializable]
public sealed partial class DropshipAttachDoAfterEvent : DropshipDoAfterEvent
{
    public DropshipAttachDoAfterEvent(NetEntity container, NetEntity contained, string slot) : base(container, contained, slot)
    {
    }
}
