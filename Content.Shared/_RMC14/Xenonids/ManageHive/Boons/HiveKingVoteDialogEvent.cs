using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.ManageHive.Boons;

[Serializable, NetSerializable]
public sealed record HiveKingVoteDialogEvent(NetEntity Cocoon, NetEntity Voted);
