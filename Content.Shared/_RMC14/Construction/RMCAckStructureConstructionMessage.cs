using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Construction;

[Serializable, NetSerializable]
public sealed class RMCAckStructureConstructionMessage : EntityEventArgs
{
    public RMCConstructionGhostKey GhostKey { get; }

    public RMCAckStructureConstructionMessage(RMCConstructionGhostKey ghostKey)
    {
        GhostKey = ghostKey;
    }
}
