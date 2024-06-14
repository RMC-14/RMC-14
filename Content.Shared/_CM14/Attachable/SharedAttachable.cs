using Robust.Shared.Serialization;
using Robust.Shared.Utility;


namespace Content.Shared._CM14.Attachable;

[Serializable, NetSerializable]
public sealed class AttachableHolderStripUserInterfaceState : BoundUserInterfaceState
{
    public Dictionary<string, string?> AttachableSlots;
    
    public AttachableHolderStripUserInterfaceState(Dictionary<string, string?> attachableSlots)
    {
        AttachableSlots = attachableSlots;
    }
}

[Serializable, NetSerializable]
public sealed class AttachableHolderChooseSlotUserInterfaceState : BoundUserInterfaceState
{
    public List<string> AttachableSlots;
    
    public AttachableHolderChooseSlotUserInterfaceState(List<string> attachableSlots)
    {
        AttachableSlots = attachableSlots;
    }
}

[Serializable, NetSerializable]
public sealed class AttachableHolderDetachMessage : BoundUserInterfaceMessage
{
    public readonly string Slot;
    
    public AttachableHolderDetachMessage(string slot)
    {
        Slot = slot;
    }
}

[Serializable, NetSerializable]
public sealed class AttachableHolderAttachToSlotMessage : BoundUserInterfaceMessage
{
    public readonly string Slot;
    
    public AttachableHolderAttachToSlotMessage(string slot)
    {
        Slot = slot;
    }
}

[Serializable, NetSerializable]
public enum AttachableHolderUiKeys : byte
{
    StripKey,
    ChooseSlotKey
}
