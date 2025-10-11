using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Construction;

[Serializable, NetSerializable]
public sealed class RMCAckStructureConstructionMessage : EntityEventArgs
{
    public int GhostId { get; }

    public RMCAckStructureConstructionMessage(int ghostId)
    {
        GhostId = ghostId;
    }
}
