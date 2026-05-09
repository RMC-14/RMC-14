using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Power.Components;
using Content.Server.Radio.Components;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Tracker.SquadLeader;
using Content.Shared._RMC14.Radio;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Language.Systems;
using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Speech;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using Robust.Shared.Utility;
using Content.Server.Administration.Logs;

namespace Content.Server.Radio.EntitySystems;

/// <summary>
///     This system handles intrinsic radios and the general process of converting radio messages into chat messages.
/// </summary>
public sealed class RadioSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!; // RMC14
    [Dependency] private readonly IChatManager _chatManager = default!; // RMC14
    [Dependency] private readonly SharedLanguageSystem _languageSystem = default!; // RMC14

    // set used to prevent radio feedback loops.
    private readonly HashSet<string> _messages = new();

    private EntityQuery<TelecomExemptComponent> _exemptQuery;

    private readonly SoundSpecifier _radioSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/radiostatic.ogg")
    {
        Params = new AudioParams
        {
            Volume = -8f,
            Variation = 0.1f,
            MaxDistance = 3.75f,
        },
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IntrinsicRadioReceiverComponent, RadioReceiveEvent>(OnIntrinsicReceive);
        SubscribeLocalEvent<IntrinsicRadioTransmitterComponent, EntitySpokeEvent>(OnIntrinsicSpeak);

        _exemptQuery = GetEntityQuery<TelecomExemptComponent>();
    }

    private void OnIntrinsicSpeak(EntityUid uid, IntrinsicRadioTransmitterComponent component, EntitySpokeEvent args)
    {
        if (args.Channel != null && component.Channels.Contains(args.Channel.ID))
        {
            SendRadioMessage(uid, args.Message, args.Channel, uid);
            args.Channel = null; // prevent duplicate messages from other listeners.
        }
    }

    private void OnIntrinsicReceive(EntityUid uid, IntrinsicRadioReceiverComponent component, ref RadioReceiveEvent args)
    {
        if (TryComp(uid, out ActorComponent? actor))
            _netMan.ServerSendMessage(args.ChatMsg, actor.PlayerSession.Channel);
    }

    /// <summary>
    /// Send radio message to all active radio listeners
    /// </summary>
    public void SendRadioMessage(EntityUid messageSource, string message, ProtoId<RadioChannelPrototype> channel, EntityUid radioSource, LanguagePrototype? language = null, bool escapeMarkup = true)
    {
        SendRadioMessage(messageSource, message, _prototype.Index(channel), radioSource, language, escapeMarkup);
    }

    /// <summary>
    /// Send radio message to all active radio listeners
    /// </summary>
    /// <param name="messageSource">Entity that spoke the message</param>
    /// <param name="radioSource">Entity that picked up the message and will send it, e.g. headset</param>
    public void SendRadioMessage(EntityUid messageSource, string message, RadioChannelPrototype channel, EntityUid radioSource, LanguagePrototype? language = null, bool escapeMarkup = true)
    {
        // TODO if radios ever garble / modify messages, feedback-prevention needs to be handled better than this.
        if (!_messages.Add(message))
            return;

        // RMC14
        var currentLanguage = language?.ID ?? _languageSystem.GetCurrentLanguage(messageSource);

        if (language != null && !language.CanUseRadio)
            return;

        bool showLanguageName = language?.ShowLanguageName ?? false;
        string? languageIcon = showLanguageName ? language?.DisplayedLanguageIcon : null;
        // RMC14

        var evt = new TransformSpeakerNameEvent(messageSource, MetaData(messageSource).EntityName);
        RaiseLocalEvent(messageSource, evt);

        var name = evt.VoiceName;
        name = FormattedMessage.EscapeText(name);

        if (TryComp(messageSource, out JobPrefixComponent? prefix))
        {
            var prefixText = (prefix.AdditionalPrefix != null ? $"{Loc.GetString(prefix.AdditionalPrefix.Value)} " : "") + Loc.GetString(prefix.Prefix);
            if (TryComp(messageSource, out SquadMemberComponent? member) &&
                TryComp(member.Squad, out SquadTeamComponent? team) &&
                team.Radio != null &&
                team.Radio != channel.ID)
            {
                name = $"({Name(member.Squad.Value)} {prefixText}) {name}";
            }
            else
            {
                if (TryComp(messageSource, out FireteamMemberComponent? fireteamMember) && fireteamMember.Fireteam >= 0)
                {
                    prefixText += $" FT{fireteamMember.Fireteam + 1}" + (TryComp(messageSource, out FireteamLeaderComponent? fireteamLeader) ? " TL" : "");
                }
                name = $"({prefixText}) {name}";
            }
        }
        else if (TryComp(messageSource, out RMCRadioPrefixComponent? radioPrefix))
        {
            var prefixText = Loc.GetString(radioPrefix.Prefix);
            name = $"{prefixText} {name}";
        }

        SpeechVerbPrototype speech;
        if (evt.SpeechVerb != null && _prototype.TryIndex(evt.SpeechVerb, out var evntProto))
            speech = evntProto;
        else
            speech = _chat.GetSpeechVerb(messageSource, message);

        var content = escapeMarkup
            ? FormattedMessage.EscapeText(message)
            : message;

        // RMC14 increase font size
        var radioFontSize = speech.FontSize;
        var radioFontId = language?.TypefaceId ?? speech.FontId; // RMC14
        if (TryComp<WearingHeadsetComponent>(messageSource, out var wearingHeadset) &&
            TryComp<RMCHeadsetComponent>(wearingHeadset.Headset, out var headsetComp))
        {
            radioFontSize += headsetComp.RadioTextIncrease ?? 0;
        }
        else if (TryComp<RMCInnateRadioTextIncreaseComponent>(messageSource, out var innateRadioIncrease))
        {
            radioFontSize += innateRadioIncrease.RadioTextIncrease;
        }

        var wrappedMessage = Loc.GetString(speech.Bold ? "chat-radio-message-wrap-bold" : "chat-radio-message-wrap",
            ("color", channel.Color),
            ("fontType", radioFontId), // RMC14
            ("fontSize", radioFontSize), // RMC14
            ("verb", Loc.GetString(_random.Pick(speech.SpeechVerbStrings))),
            ("channel", $"\\[{channel.LocalizedName}\\]"),
            ("name", name),
            ("message", content));

        var sendAttemptEv = new RadioSendAttemptEvent(channel, radioSource);
        RaiseLocalEvent(ref sendAttemptEv);
        RaiseLocalEvent(radioSource, ref sendAttemptEv);
        var canSend = !sendAttemptEv.Cancelled;

        var sourceMapId = Transform(radioSource).MapID;
        var hasActiveServer = HasActiveServer(sourceMapId, channel.ID);
        var sourceServerExempt = _exemptQuery.HasComp(radioSource);

        var radioQuery = EntityQueryEnumerator<ActiveRadioComponent, TransformComponent>();
        while (canSend && radioQuery.MoveNext(out var receiver, out var radio, out var transform))
        {
            if (!radio.ReceiveAllChannels)
            {
                if (!radio.Channels.Contains(channel.ID) || (TryComp<IntercomComponent>(receiver, out var intercom) &&
                                                             !intercom.SupportedChannels.Contains(channel.ID)))
                    continue;
            }

            if (!channel.LongRange && transform.MapID != sourceMapId && !radio.GlobalReceive)
                continue;

            // don't need telecom server for long range channels or handheld radios and intercoms
            var needServer = !channel.LongRange && !sourceServerExempt;
            if (needServer && !hasActiveServer)
                continue;

            // check if message can be sent to specific receiver
            var attemptEv = new RadioReceiveAttemptEvent(channel, radioSource, receiver);
            RaiseLocalEvent(ref attemptEv);
            RaiseLocalEvent(receiver, ref attemptEv);
            if (attemptEv.Cancelled)
                continue;

            // send the message
            //RMC - because we cant send the same msg to everyone - tho i think there is better way
            string actualMessage = message;
            string actualWrappedMessage = wrappedMessage;
            string? actualLanguageIcon = languageIcon;

            EntityUid? listenerEntity = null;
            if (TryComp<IntrinsicRadioReceiverComponent>(receiver, out var intrinsicReceiver))
            {
                listenerEntity = receiver;
            }
            else
            {
                var query = EntityQueryEnumerator<WearingHeadsetComponent>();
                while (query.MoveNext(out var wearer, out var wearing))
                {
                    if (wearing.Headset == receiver)
                    {
                        listenerEntity = wearer;
                        break;
                    }
                }
            }

            if (listenerEntity.HasValue && !_languageSystem.CanUnderstand(listenerEntity.Value, currentLanguage))
            {
                var obfuscatedMessage = _languageSystem.ObfuscateMessage(message, currentLanguage);
                actualMessage = obfuscatedMessage;

                actualWrappedMessage = Loc.GetString(speech.Bold ? "chat-radio-message-wrap-bold" : "chat-radio-message-wrap",
                    ("color", channel.Color),
                    ("fontType", radioFontId),
                    ("fontSize", radioFontSize),
                    ("verb", Loc.GetString(_random.Pick(speech.SpeechVerbStrings))),
                    ("channel", $"\\[{channel.LocalizedName}\\]"),
                    ("name", name),
                    ("message", escapeMarkup ? FormattedMessage.EscapeText(obfuscatedMessage) : obfuscatedMessage));
            }

            var chat = new ChatMessage(
                ChatChannel.Radio,
                actualMessage,
                actualWrappedMessage,
                GetNetEntity(messageSource),
                _chatManager.EnsurePlayer(CompOrNull<ActorComponent>(messageSource)?.PlayerSession.UserId)?.Key,
                languageIcon: actualLanguageIcon,
                repeatCheckSender: !HasComp<ChatRepeatIgnoreSenderComponent>(radioSource));

            var chatMsg = new MsgChatMessage { Message = chat };
            var ev = new RadioReceiveEvent(actualMessage, messageSource, channel, radioSource, chatMsg);
            //RMC
            RaiseLocalEvent(receiver, ref ev);
        }

        if (canSend &&
            !HasComp<XenoComponent>(messageSource) &&
            HasComp<RMCHeadsetComponent>(radioSource))
        {
            var filter = Filter.Pvs(messageSource).RemoveWhereAttachedEntity(HasComp<XenoComponent>);
            _audio.PlayEntity(_radioSound, filter, messageSource, false); // RMC14
        }

        if (name != Name(messageSource))
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(messageSource):user} as {name} on {channel.LocalizedName} in {currentLanguage}: {message}");
        else
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(messageSource):user} on {channel.LocalizedName} in {currentLanguage}: {message}");

        var replayChat = new ChatMessage(
            ChatChannel.Radio,
            message,
            wrappedMessage,
            GetNetEntity(messageSource),
            _chatManager.EnsurePlayer(CompOrNull<ActorComponent>(messageSource)?.PlayerSession.UserId)?.Key,
            languageIcon: languageIcon,
            repeatCheckSender: !HasComp<ChatRepeatIgnoreSenderComponent>(radioSource));
        _replay.RecordServerMessage(replayChat);

        _messages.Remove(message);
    }

    /// <inheritdoc cref="TelecomServerComponent"/>
    private bool HasActiveServer(MapId mapId, string channelId)
    {
        var servers = EntityQuery<TelecomServerComponent, EncryptionKeyHolderComponent, ApcPowerReceiverComponent, TransformComponent>();
        foreach (var (_, keys, power, transform) in servers)
        {
            if (transform.MapID == mapId &&
                power.Powered &&
                keys.Channels.Contains(channelId))
            {
                return true;
            }
        }
        return false;
    }
}
