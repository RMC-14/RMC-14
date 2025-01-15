using Content.Server.Chat.Managers;
using Content.Server.Hands.Systems;
using Content.Server.Radio;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Shared._RMC14.Communications;
using Content.Shared._RMC14.Hands;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Telephone;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Chat;
using Content.Shared.Coordinates;
using Robust.Server.Audio;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Telephone;

public sealed class TelephoneSystem : SharedTelephoneSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly CommunicationsTowerSystem _communicationsTower = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly CMHandsSystem _rmcHands = default!;
    [Dependency] private readonly RMCPlanetSystem _rmcPlanet = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioSendAttemptEvent>(OnRadioSendAttempt);
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);

        SubscribeLocalEvent<TelephoneComponent, ListenEvent>(OnListen);
    }

    private void OnRadioSendAttempt(ref RadioSendAttemptEvent ev)
    {
        if (!_rmcPlanet.IsOnPlanet(ev.RadioSource.ToCoordinates()))
            return;

        if (!ev.Channel.Planet)
        {
            ev.Cancelled = true;
            return;
        }

        if (ev.Channel.Tower &&
            !_communicationsTower.CanTransmit(ev.Channel))
        {
            ev.Cancelled = true;
        }
    }

    private void OnRadioReceiveAttempt(ref RadioReceiveAttemptEvent ev)
    {
        if (!_rmcPlanet.IsOnPlanet(ev.RadioReceiver.ToCoordinates()))
            return;

        if (!ev.Channel.Planet)
        {
            ev.Cancelled = true;
            return;
        }

        if (ev.Channel.Tower &&
            !_communicationsTower.CanTransmit(ev.Channel))
        {
            ev.Cancelled = true;
        }
    }

    private void OnListen(Entity<TelephoneComponent> ent, ref ListenEvent args)
    {
        if (HasComp<TelephoneComponent>(args.Source) || HasComp<XenoComponent>(args.Source))
            return;

        if (!_hands.IsHolding(args.Source, ent))
            return;

        if (ent.Comp.RotaryPhone is not { } rotary ||
            !TryGetOtherPhone(rotary, out var otherPhone) ||
            !_rmcHands.TryGetHolder(otherPhone, out var holder) ||
            !TryComp(holder, out ActorComponent? actor))
        {
            return;
        }

        var name = GetPhoneName(rotary);
        var message = $"{name} says, \"{FormattedMessage.EscapeText(args.Message)}\"";
        var sound = _audio.GetSound(ent.Comp.SpeakSound);
        _chatManager.ChatMessageToOne(ChatChannel.Local, message, message, otherPhone, false, actor.PlayerSession.Channel, Color.FromHex("#9956D3"), true, sound, -12, hidePopup: true);
    }

    protected override void PickupPhone(Entity<RotaryPhoneComponent> rotary, EntityUid telephone, EntityUid user)
    {
        base.PickupPhone(rotary, telephone, user);
        EnsureComp<ActiveListenerComponent>(telephone);
    }
}
