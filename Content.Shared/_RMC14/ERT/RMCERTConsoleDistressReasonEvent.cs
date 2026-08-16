using Content.Shared._RMC14.Dialog;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.ERT;

/// <summary>
/// Dialog callback for console distress requests.
/// </summary>
/// <param name="User">Player entity that submitted the console request.</param>
/// <param name="Message">Reason text entered into the dialog.</param>
[Serializable, NetSerializable]
public sealed record RMCERTConsoleDistressReasonEvent(NetEntity User, string Message = "") : DialogInputEvent(Message);
