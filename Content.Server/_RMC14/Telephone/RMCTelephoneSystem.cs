using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Radio;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Shared._RMC14.Communications;
using Content.Shared._RMC14.Hands;
using Content.Shared._RMC14.Radio;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Telephone;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Chat;
using Content.Shared.Coordinates;
using Robust.Server.Audio;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Telephone;

public sealed class RMCTelephoneSystem : SharedRMCTelephoneSystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly CommunicationsTowerSystem _communicationsTower = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly RMCHandsSystem _rmcHands = default!;
    [Dependency] private readonly RMCPlanetSystem _rmcPlanet = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioSendAttemptEvent>(OnRadioSendAttempt);
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);

        SubscribeLocalEvent<RMCTelephoneComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<RMCTelephoneRingEvent>(OnTelephoneRing);
    }

    private void OnRadioSendAttempt(ref RadioSendAttemptEvent ev)
    {
        if (TryComp<RMCRadioFilterComponent>(ev.RadioSource, out var filter))
        {
            if (filter.DisabledChannels.Contains(ev.Channel.ID))
                ev.Cancelled = true;
        }

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
        if (TryComp<RMCRadioFilterComponent>(ev.RadioReceiver, out var filter))
        {
            if (filter.DisabledChannels.Contains(ev.Channel.ID))
                ev.Cancelled = true;
        }

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

    private void OnListen(Entity<RMCTelephoneComponent> ent, ref ListenEvent args)
    {
        OnListen(ent, args.Source, args.Message);
    }

    protected override void PickupPhone(Entity<RotaryPhoneComponent> rotary, EntityUid telephone, EntityUid user)
    {
        base.PickupPhone(rotary, telephone, user);
        EnsureComp<ActiveListenerComponent>(telephone);
    }

    private void OnTelephoneRing(ref RMCTelephoneRingEvent ev)
    {
        if (TryComp<RotaryPhoneBackpackComponent>(ev.Receiving, out var comp))
        {
            _chat.TrySendInGameICMessage(ev.Receiving, "rings vigorously!", InGameICChatType.Emote, false, ignoreActionBlocker: true);
        }
        else
        {
            _chat.TrySendInGameICMessage(ev.Receiving, "phone rings vigorously!", InGameICChatType.Emote, false, ignoreActionBlocker: true);
        }

        if (TryComp<RotaryPhoneComponent>(ev.Receiving, out var phone) && phone.NotifyAdmins)
        {
            _chatManager.SendAdminAnnouncement(Loc.GetString("admin-call-incoming", ("actor", Name(ev.Actor)), ("from", Name(ev.Calling)), ("to", Name(ev.Receiving))));
        }
    }

    public void OnListen(Entity<RMCTelephoneComponent> ent, EntityUid source, string message)
    {
        if (HasComp<RMCTelephoneComponent>(source) || HasComp<XenoComponent>(source))
            return;

        if (!_hands.IsHolding(source, ent))
            return;

        if (ent.Comp.RotaryPhone is not { } rotary ||
            !TryGetOtherPhone(rotary, out var otherPhone) ||
            !_rmcHands.TryGetHolder(otherPhone, out var holder) ||
            !TryComp(holder, out ActorComponent? actor))
        {
            return;
        }

        var name = GetPhoneName(rotary);
        message = $"{name} says, \"{FormattedMessage.EscapeText(message)}\"";
        var sound = _audio.GetSound(ent.Comp.SpeakSound);
        _chatManager.ChatMessageToOne(ChatChannel.Local, message, message, otherPhone, false, actor.PlayerSession.Channel, Color.FromHex("#9956D3"), true, sound, -12, hidePopup: true);
    }
}
