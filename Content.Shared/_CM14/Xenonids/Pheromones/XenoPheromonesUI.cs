using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Xenonids.Pheromones;

[Serializable, NetSerializable]
public enum XenoPheromonesUI
{
    Key
}

[Serializable, NetSerializable]
public sealed class XenoPheromonesChosenBuiMsg(XenoPheromones pheromones) : BoundUserInterfaceMessage
{
    public readonly XenoPheromones Pheromones = pheromones;
}
