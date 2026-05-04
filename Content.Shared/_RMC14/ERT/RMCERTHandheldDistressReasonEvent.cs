using Content.Shared._RMC14.Dialog;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.ERT;

/// <summary>
/// Dialog callback for handheld distress beacons.
/// </summary>
/// <param name="Message">Reason text entered into the dialog.</param>
[Serializable, NetSerializable]
public sealed record RMCERTHandheldDistressReasonEvent(string Message = "") : DialogInputEvent(Message);
