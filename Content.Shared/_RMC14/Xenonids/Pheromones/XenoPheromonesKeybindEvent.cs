using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Pheromones;

[Serializable, NetSerializable]
public sealed class XenoPheromonesKeybindEvent(XenoPheromones pheromones) : EntityEventArgs
{
    public readonly XenoPheromones Pheromones = pheromones;
}
