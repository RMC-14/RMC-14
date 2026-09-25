using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Pushup;

[Serializable, NetSerializable]
public sealed partial class RMCPushupDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed class RMCPushupSelectedEvent(RMCPushupForm form) : EntityEventArgs
{
    public readonly RMCPushupForm Form = form;
}

[ByRefEvent]
public readonly record struct RMCPushupVisualsChangedEvent;
