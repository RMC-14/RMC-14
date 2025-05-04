using Content.Shared._RMC14.Dialog;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.ManageHive;

[Serializable, NetSerializable]
public sealed record ManageHiveDevolveMessageEvent(EntProtoId Choice, string Message = "") : DialogInputEvent(Message);
