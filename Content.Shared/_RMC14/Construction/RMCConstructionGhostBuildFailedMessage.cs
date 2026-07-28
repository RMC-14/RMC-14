using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Construction;

[Serializable, NetSerializable]
public sealed class RMCConstructionGhostBuildFailedMessage : EntityEventArgs
{
    public RMCConstructionGhostKey GhostKey { get; }
    public RMCConstructionFailureReason Reason { get; }

    public RMCConstructionGhostBuildFailedMessage(RMCConstructionGhostKey ghostKey, RMCConstructionFailureReason reason = RMCConstructionFailureReason.Unknown)
    {
        GhostKey = ghostKey;
        Reason = reason;
    }
}
