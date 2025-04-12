using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Admin;

[Serializable, NetSerializable]
public sealed record SpawnAsJobDialogEvent(NetEntity User, NetEntity Target, string JobId);
