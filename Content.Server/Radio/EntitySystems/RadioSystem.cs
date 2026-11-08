using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Power.Components;
using Content.Server.Radio.Components;
using Content.Server._RMC14.Language.Systems;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Radio;
using Content.Shared._RMC14.Tracker.SquadLeader;
using Content.Shared._RMC14.Xenonids;
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
    // RMC14
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly LanguageSystem _language = default!;
    // RMC14

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
    }; // RMC14

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IntrinsicRadioReceiverComponent, RadioReceiveEvent>(OnIntrinsicReceive);
        SubscribeLocalEvent<IntrinsicRadioTransmitterComponent, EntitySpokeEvent>(OnIntrinsicSpeak);

        _exemptQuery = GetEntityQuery<TelecomExemptComponent>();
    }

    // RMC14
    private void OnIntrinsicSpeak(Entity<IntrinsicRadioTransmitterComponent> ent, ref EntitySpokeEvent args)
    {
        if (args.Channel != null && ent.Comp.Channels.Contains(args.Channel.ID))
        {
            SendRadioMessage(ent.Owner, args.Message, args.Channel, ent.Owner, args.Language);
            args.Channel = null; // prevent duplicate messages from other listeners.
        }
    }
    // RMC14

    // RMC14
    private void OnIntrinsicReceive(Entity<IntrinsicRadioReceiverComponent> ent, ref RadioReceiveEvent args)
    {
        if (TryComp(ent.Owner, out ActorComponent? actor))
            _netMan.ServerSendMessage(args.ChatMsg, actor.PlayerSession.Channel);
    }
    // RMC14

    /// <summary>
    /// Send radio message to all active radio listeners
    /// </summary>
    // RMC14
    public void SendRadioMessage(
        EntityUid messageSource,
        string message,
        ProtoId<RadioChannelPrototype> channel,
        EntityUid radioSource,
        ProtoId<LanguagePrototype>? language = null,
        bool escapeMarkup = true)
    {
        SendRadioMessage(messageSource, message, _prototype.Index(channel), radioSource, language, escapeMarkup);
    }
    // RMC14

    /// <summary>
    /// Send radio message to all active radio listeners
    /// </summary>
    /// <param name="messageSource">Entity that spoke the message</param>
    /// <param name="radioSource">Entity that picked up the message and will send it, e.g. headset</param>
    // RMC14
    public void SendRadioMessage(
        EntityUid messageSource,
        string message,
        RadioChannelPrototype channel,
        EntityUid radioSource,
        ProtoId<LanguagePrototype>? language = null,
        bool escapeMarkup = true)
    {
        // TODO if radios ever garble / modify messages, feedback-prevention needs to be handled better than this.
        if (!_messages.Add(message))
            return;

        try
        {
            // RMC14
            var currentLanguage = language ?? _language.GetCurrentLanguage(messageSource);
            _prototype.TryIndex(currentLanguage, out LanguagePrototype? languagePrototype);

            if (languagePrototype != null && !languagePrototype.CanUseRadio)
                return;

            var showLanguageName = languagePrototype?.ShowLanguageName ?? false;
            var languageIcon = showLanguageName ? languagePrototype?.DisplayedLanguageIcon : null;
            // RMC14

            var evt = new TransformSpeakerNameEvent(messageSource, MetaData(messageSource).EntityName);
            RaiseLocalEvent(messageSource, evt);

            var name = evt.VoiceName;

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

            // RMC14
            SpeechVerbPrototype speech;
            // RMC14
            if (evt.SpeechVerb != null && _prototype.TryIndex(evt.SpeechVerb, out var evntProto))
                speech = evntProto;
            else
                speech = _chat.GetSpeechVerb(messageSource, message);

            var content = escapeMarkup
                ? FormattedMessage.EscapeText(message)
                : message;

            // RMC14
            var radioFontSize = speech.FontSize;
            var radioFontId = languagePrototype?.TypefaceId ?? speech.FontId;
            // RMC14
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
                // RMC14
                ("fontType", radioFontId),
                ("fontSize", radioFontSize),
                // RMC14
                ("verb", Loc.GetString(_random.Pick(speech.SpeechVerbStrings))),
                ("channel", $"\\[{channel.LocalizedName}\\]"),
                ("name", FormattedMessage.EscapeText(name)),
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
                // RMC14
                var actualMessage = message;
                var actualWrappedMessage = wrappedMessage;
                string? actualLanguageIcon = languageIcon;

                var listenerEntity = ResolveRadioListener(receiver);

                if (listenerEntity.HasValue && !_language.CanUnderstand(listenerEntity.Value, currentLanguage))
                {
                    var obfuscatedMessage = _language.ObfuscateMessageForListener(listenerEntity.Value, message, currentLanguage);
                    actualMessage = obfuscatedMessage;

                    var actualName = _chat.GetSpeakerNameForListener(messageSource, listenerEntity, name);
                    actualWrappedMessage = Loc.GetString(speech.Bold ? "chat-radio-message-wrap-bold" : "chat-radio-message-wrap",
                        ("color", channel.Color),
                        ("fontType", radioFontId),
                        ("fontSize", radioFontSize),
                        ("verb", Loc.GetString(_random.Pick(speech.SpeechVerbStrings))),
                        ("channel", $"\\[{channel.LocalizedName}\\]"),
                        ("name", FormattedMessage.EscapeText(actualName)),
                        ("message", escapeMarkup ? FormattedMessage.EscapeText(obfuscatedMessage) : obfuscatedMessage));
                }
                else if (listenerEntity.HasValue)
                {
                    var actualName = _chat.GetSpeakerNameForListener(messageSource, listenerEntity, name);
                    actualWrappedMessage = Loc.GetString(speech.Bold ? "chat-radio-message-wrap-bold" : "chat-radio-message-wrap",
                        ("color", channel.Color),
                        ("fontType", radioFontId),
                        ("fontSize", radioFontSize),
                        ("verb", Loc.GetString(_random.Pick(speech.SpeechVerbStrings))),
                        ("channel", $"\\[{channel.LocalizedName}\\]"),
                        ("name", FormattedMessage.EscapeText(actualName)),
                        ("message", escapeMarkup ? FormattedMessage.EscapeText(actualMessage) : actualMessage));
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
                var ev = new RadioReceiveEvent(actualMessage, messageSource, channel, radioSource, chatMsg, currentLanguage);
                // RMC14
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
        }
        finally
        {
            _messages.Remove(message);
        }
    }
    // RMC14

    private EntityUid? ResolveRadioListener(EntityUid receiver)
    {
        if (HasComp<IntrinsicRadioReceiverComponent>(receiver))
            return receiver;

        var wearer = Transform(receiver).ParentUid;
        if (wearer.IsValid() &&
            TryComp<WearingHeadsetComponent>(wearer, out var wearing) &&
            wearing.Headset == receiver)
        {
            return wearer;
        }

        return null;
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
