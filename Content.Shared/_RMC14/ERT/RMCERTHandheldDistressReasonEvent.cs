using Content.Shared._RMC14.Dialog;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.ERT;

/// <summary>
/// Dialog callback for handheld distress beacons.
/// </summary>
/// <param name="Beacon">Distress beacon that opened the dialog.</param>
/// <param name="Message">Reason text entered into the dialog.</param>
[Serializable, NetSerializable]
public sealed record RMCERTHandheldDistressReasonEvent(NetEntity Beacon, string Message = "") : DialogInputEvent(Message);
