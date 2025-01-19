using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Overwatch;

[Serializable, NetSerializable]
public sealed record OverwatchTransferMarineSquadEvent(NetEntity Actor, NetEntity Marine, NetEntity Squad);
