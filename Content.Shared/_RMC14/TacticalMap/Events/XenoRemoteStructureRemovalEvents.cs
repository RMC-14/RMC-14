using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.TacticalMap;

[Serializable, NetSerializable]
public sealed record XenoRemoteStructureRemovalConfirmEvent(NetEntity Actor, NetEntity Target, bool Cancel);
