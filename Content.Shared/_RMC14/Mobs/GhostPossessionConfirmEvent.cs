using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Mobs;

[Serializable, NetSerializable]
public sealed record GhostPossessionConfirmEvent(NetEntity Actor, NetEntity Possessor, NetEntity ToPossess);
