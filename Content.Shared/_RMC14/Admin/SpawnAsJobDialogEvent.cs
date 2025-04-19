using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Admin;

[Serializable, NetSerializable]
public sealed record SpawnAsJobDialogEvent(NetEntity User, NetEntity Target, ProtoId<JobPrototype> JobId);
