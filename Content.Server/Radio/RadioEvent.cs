using Content.Shared.Chat;
// RMC14
using Content.Shared._RMC14.Language.Prototypes;
// RMC14
using Content.Shared.Radio;
// RMC14
using Robust.Shared.Prototypes;
// RMC14

namespace Content.Server.Radio;

[ByRefEvent]
// RMC14
public readonly record struct RadioReceiveEvent(
    string Message,
    EntityUid MessageSource,
    RadioChannelPrototype Channel,
    EntityUid RadioSource,
    MsgChatMessage ChatMsg,
    ProtoId<LanguagePrototype> Language
);
// RMC14

/// <summary>
/// Event raised on the parent entity of a headset radio when a radio message is received
/// </summary>
[ByRefEvent]
public readonly record struct HeadsetRadioReceiveRelayEvent(RadioReceiveEvent RelayedEvent);

/// <summary>
/// Use this event to cancel sending message per receiver
/// </summary>
[ByRefEvent]
public record struct RadioReceiveAttemptEvent(RadioChannelPrototype Channel, EntityUid RadioSource, EntityUid RadioReceiver)
{
    public readonly RadioChannelPrototype Channel = Channel;
    public readonly EntityUid RadioSource = RadioSource;
    public readonly EntityUid RadioReceiver = RadioReceiver;
    public bool Cancelled = false;
}

/// <summary>
/// Use this event to cancel sending message to every receiver
/// </summary>
[ByRefEvent]
public record struct RadioSendAttemptEvent(RadioChannelPrototype Channel, EntityUid RadioSource)
{
    public readonly RadioChannelPrototype Channel = Channel;
    public readonly EntityUid RadioSource = RadioSource;
    public bool Cancelled = false;
}
