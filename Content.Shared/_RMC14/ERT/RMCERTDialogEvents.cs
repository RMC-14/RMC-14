using Content.Shared._RMC14.Dialog;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.ERT;

[Serializable, NetSerializable]
/// <summary>
/// Dialog callback for console distress requests.
/// </summary>
public sealed record RMCERTConsoleDistressReasonEvent(NetEntity User, string Message = "") : DialogInputEvent(Message);

[Serializable, NetSerializable]
/// <summary>
/// Dialog callback for handheld distress beacons.
/// </summary>
public sealed record RMCERTHandheldDistressReasonEvent(string Message = "") : DialogInputEvent(Message);
