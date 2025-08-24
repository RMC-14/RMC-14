using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Construction;

[Serializable, NetSerializable]
public sealed class RMCConstructionGhostBuildFailedMessage : EntityEventArgs
{
    public int GhostId { get; }

    public RMCConstructionGhostBuildFailedMessage(int ghostId)
    {
        GhostId = ghostId;
    }
}
