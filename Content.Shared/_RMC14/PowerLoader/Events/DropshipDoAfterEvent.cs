using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.PowerLoader.Events;

[Serializable, NetSerializable]
public abstract partial class DropshipDoAfterEvent : SimpleDoAfterEvent
{
    public NetEntity Container;
    public NetEntity Contained;
    public string Slot;

    protected DropshipDoAfterEvent(NetEntity container, NetEntity contained, string slot)
    {
        Container = container;
        Contained = contained;
        Slot = slot;
    }
}
