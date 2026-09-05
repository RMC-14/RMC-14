using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Photocopier.Events;
[Serializable, NetSerializable]
public sealed class CopiedPaperEvent : BoundUserInterfaceMessage
{
    public int CopyCount;
}

[Serializable, NetSerializable]
public sealed class EjectPaperEvent : BoundUserInterfaceMessage
{

}
