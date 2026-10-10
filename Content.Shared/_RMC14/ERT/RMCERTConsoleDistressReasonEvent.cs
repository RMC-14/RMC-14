using Content.Shared._RMC14.Dialog;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.ERT;

/// <summary>
/// Dialog callback for console distress requests.
/// </summary>
/// <param name="Console">Console from which the player opened the request dialog.</param>
/// <param name="Message">Reason text entered into the dialog.</param>
[Serializable, NetSerializable]
public sealed record RMCERTConsoleDistressReasonEvent(NetEntity Console, string Message = "") : DialogInputEvent(Message);
