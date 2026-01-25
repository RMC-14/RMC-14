using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vendors;

[Serializable, NetSerializable]
public enum CMAutomatedVendorUI : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class CMVendorVendBuiMsg(int section, int entry, List<int> linkedEntries) : BoundUserInterfaceMessage
{
    public readonly int Section = section;
    public readonly int Entry = entry;
    public readonly List<int> LinkedEntries = linkedEntries;
}

[Serializable, NetSerializable]
public sealed class CMVendorRefreshBuiMsg : BoundUserInterfaceMessage;
