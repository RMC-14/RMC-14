using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Storage;

[Serializable, NetSerializable]
public sealed partial class OpenStorageDoAfterEvent : DoAfterEvent
{
    public readonly NetEntity Uid;
    public readonly NetEntity Entity;
    public readonly bool Silent;

    public OpenStorageDoAfterEvent(NetEntity uid, NetEntity entity, bool silent)
    {
        Uid = uid;
        Entity = entity;
        Silent = silent;
    }

    public override DoAfterEvent Clone()
    {
        return new OpenStorageDoAfterEvent(Uid, Entity, Silent);
    }
}
