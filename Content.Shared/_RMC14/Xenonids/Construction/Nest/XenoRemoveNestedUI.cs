using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Construction.Nest;

[Serializable, NetSerializable]
public enum XenoRemoveNestedUI : byte
{
    Key
}
[Serializable, NetSerializable]
public sealed class XenoRemoveNestedBuiMsg(bool removeNested) : BoundUserInterfaceMessage
{
    public readonly bool RemoveNested = removeNested;
}
