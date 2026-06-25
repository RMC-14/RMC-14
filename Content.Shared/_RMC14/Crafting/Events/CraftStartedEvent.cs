using Robust.Shared.Serialization;

namespace Content.Shared.Crafting.Events;

[Serializable, NetSerializable]
public sealed class CraftStartedEvent : EntityEventArgs
{
    public readonly NetEntity StorageEnt;

    public CraftStartedEvent(NetEntity storageEnt)
    {
        StorageEnt = storageEnt;
    }
}
