using Content.Shared._RMC14.Dialog;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.ERT;

[Serializable, NetSerializable]
public sealed record RMCERTConsoleDistressReasonEvent(NetEntity User, string Message = "") : DialogInputEvent(Message);

[Serializable, NetSerializable]
public sealed record RMCERTHandheldDistressReasonEvent(NetEntity User, string Message = "") : DialogInputEvent(Message);
