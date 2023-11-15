using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Xenos.Pheromones;

[Serializable, NetSerializable]
public enum XenoPheromonesUI
{
    Key
}

[Serializable, NetSerializable]
public sealed class XenoPheromonesChosenBuiMessage : BoundUserInterfaceMessage
{
    public readonly XenoPheromones Pheromones;

    public XenoPheromonesChosenBuiMessage(XenoPheromones pheromones)
    {
        Pheromones = pheromones;
    }
}
