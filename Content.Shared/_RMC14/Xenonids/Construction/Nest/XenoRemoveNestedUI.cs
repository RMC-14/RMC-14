using Robust.Shared.Serialization;
using Robust.Shared.GameObjects;

namespace Content.Shared._RMC14.Xenonids.Construction.Nest;

[Serializable, NetSerializable]
public enum XenoRemoveNestedUI : byte
{
    Key
}
[Serializable, NetSerializable]
public sealed class XenoRemoveNestedBuiMsg() : BoundUserInterfaceMessage
{
}

