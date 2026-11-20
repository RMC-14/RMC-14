using Content.Shared._RMC14.Dialog;
using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Psychic;

public sealed partial class XenoPsychicActionEvent : WorldTargetActionEvent;

[Serializable, NetSerializable]
public sealed record XenoPsychicWhisperInputEvent(NetEntity Action, NetEntity Target, string Message = "")
    : DialogInputEvent(Message);

[Serializable, NetSerializable]
public sealed record XenoPsychicRadianceInputEvent(NetEntity Action, string Message = "")
    : DialogInputEvent(Message);

[Serializable, NetSerializable]
public sealed record XenoGiveOrderInputEvent(NetEntity Action, NetEntity Target, string Message = "")
    : DialogInputEvent(Message);
