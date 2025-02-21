using Content.Shared._RMC14.Dialog;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.Announce;

[Serializable, NetSerializable]
public sealed record EchoSquadReasonEvent(NetEntity User, string Message = "") : DialogInputEvent(Message);
