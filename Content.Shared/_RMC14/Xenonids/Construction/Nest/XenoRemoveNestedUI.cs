using Robust.Shared.Serialization;
using Robust.Shared.GameObjects;

namespace Content.Shared._RMC14.Xenonids.Construction.Nest;

[Serializable, NetSerializable]
public enum XenoRemoveNestedUI : byte
{
    Key
}
[Serializable, NetSerializable]
public sealed class XenoRemoveNestedBuiMsg(bool removeNested, int nestableTarget) : BoundUserInterfaceMessage
{
    public readonly bool RemoveNested = removeNested;
    public readonly int NestableTarget = nestableTarget;
}
[Serializable, NetSerializable]
public sealed class RemoveNestedState : BoundUserInterfaceState
{
    public readonly int NestableTarget;

    public RemoveNestedState(int target)
    {
        NestableTarget = target;
    }
}
