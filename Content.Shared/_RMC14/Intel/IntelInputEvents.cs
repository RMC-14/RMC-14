using Content.Shared._RMC14.Dialog;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Intel;

[Serializable, NetSerializable]
public sealed record IntelDiskReaderKeyInputEvent(NetEntity User, NetEntity Disk, string Message = "") : DialogInputEvent(Message);

[Serializable, NetSerializable]
public sealed record IntelDataTerminalPasswordInputEvent(NetEntity User, string Message = "") : DialogInputEvent(Message);

[Serializable, NetSerializable]
public sealed record IntelSafeCodeInputEvent(NetEntity User, string Message = "") : DialogInputEvent(Message);
