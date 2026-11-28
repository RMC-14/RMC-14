using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.Banish;

/// <summary>
/// players banished from this hive, kept after they leave their body so they can't rejoin it
/// as a lesser drone or parasite, only through the larva queue or by being readmitted
/// </summary>
[RegisterComponent]
public sealed partial class HiveBanishedPlayersComponent : Component
{
    [ViewVariables]
    public HashSet<NetUserId> Players = new();
}
