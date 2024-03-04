using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Vendors;

[Serializable, NetSerializable]
public enum CMAutomatedVendorUI : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class CMVendorVendBuiMessage : BoundUserInterfaceMessage
{
    public readonly int Section;
    public readonly int Entry;

    public CMVendorVendBuiMessage(int section, int entry)
    {
        Section = section;
        Entry = entry;
    }
}

[Serializable, NetSerializable]
public sealed class CMVendorRefreshBuiMessage : BoundUserInterfaceMessage;
