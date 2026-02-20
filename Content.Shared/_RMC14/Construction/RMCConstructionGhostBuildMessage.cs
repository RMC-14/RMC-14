using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Construction;

[Serializable, NetSerializable]
public sealed class RMCConstructionGhostBuildMessage : EntityEventArgs
{
    public int Amount { get; }
    public RMCConstructionGhostKey GhostKey { get; }

    public RMCConstructionGhostBuildMessage(int amount, RMCConstructionGhostKey ghostKey)
    {
        Amount = amount;
        GhostKey = ghostKey;
    }
}
