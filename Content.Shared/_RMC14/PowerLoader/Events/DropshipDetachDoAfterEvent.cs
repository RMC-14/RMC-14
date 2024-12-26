using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.PowerLoader;

[Serializable, NetSerializable]
public sealed partial class DropshipDetachDoAfterEvent : SimpleDoAfterEvent
{
    public NetEntity ContainerEntity;

    public NetEntity ContainedEntity;

    public DropshipDetachDoAfterEvent(NetEntity containerEntity, NetEntity containedEntity)
    {
        ContainerEntity = containerEntity;
        ContainedEntity = containedEntity;
    }
}
