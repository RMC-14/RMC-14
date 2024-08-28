using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.PowerLoader;

[Serializable, NetSerializable]
public sealed partial class DropshipAttachDoAfterEvent : SimpleDoAfterEvent
{
    public NetEntity ContainerEntity;

    public NetEntity ContainedEntity;

    public string SlotId;

    public DropshipAttachDoAfterEvent(NetEntity containerEntity, NetEntity containedEntity, string slotId)
    {
        ContainerEntity = containerEntity;
        ContainedEntity = containedEntity;
        SlotId = slotId;
    }

    public override DoAfterEvent Clone()
    {
        DropshipAttachDoAfterEvent newDoAfter = (DropshipAttachDoAfterEvent) base.Clone();
        newDoAfter.ContainerEntity = this.ContainerEntity;
        newDoAfter.ContainedEntity = this.ContainedEntity;
        newDoAfter.SlotId = this.SlotId;
        return newDoAfter;
    }
}
