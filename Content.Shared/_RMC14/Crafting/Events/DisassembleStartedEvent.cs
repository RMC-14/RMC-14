using Robust.Shared.Serialization;

namespace Content.Shared.Crafting.Events;

[Serializable, NetSerializable]
public sealed class DisassembleStartedEvent : EntityEventArgs
{
    public readonly NetEntity StorageEnt;

    public DisassembleStartedEvent(NetEntity storageEnt)
    {
        StorageEnt = storageEnt;
    }
}
