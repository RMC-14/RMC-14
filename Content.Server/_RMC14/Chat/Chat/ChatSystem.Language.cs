using Content.Server._RMC14.Chat.Chat;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.IdentityManagement;
using Content.Shared._RMC14.Language;
using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared._RMC14.Language.Systems;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Players;
using Content.Shared.Radio;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    private ProtoId<LanguagePrototype> GetCurrentLanguageForSpeech(EntityUid source)
    {
        var currentLanguage = _language.GetCurrentLanguage(source);

        var languageEv = new DetermineLanguageEvent(source, currentLanguage);
        RaiseLocalEvent(source, ref languageEv);
        return languageEv.Language;
    }

    public string GetSpeakerNameForListener(EntityUid source, EntityUid? listener, string transformedName)
    {
        if (listener == null)
            return transformedName;

        if (TryComp<FixedIdentityComponent>(source, out var fixedIdentity) &&
            fixedIdentity.Name != null &&
            _whitelistSystem.IsWhitelistPass(fixedIdentity.Whitelist, listener.Value))
        {
            return Identity.Name(source, EntityManager, listener.Value).Name;
        }

        return transformedName;
    }

    private void SendEntitySpeakWithLanguage(
        EntityUid source,
        string originalMessage,
        ChatTransmitRange range,
        string? nameOverride,
        bool hideLog,
        bool ignoreActionBlocker,
        ProtoId<LanguagePrototype> language)
    {
        LanguagePrototype? languagePrototype = null;
        if (!_prototypeManager.TryIndex(language, out languagePrototype))
        {
            language = SharedLanguageSystem.CommonLanguage;
            _prototypeManager.TryIndex(language, out languagePrototype);
        }

        var needsLos = languagePrototype?.NeedsLOS ?? false;
        var needsSpeech = languagePrototype?.NeedsSpeech ?? true;

        if (needsSpeech && !_actionBlocker.CanSpeak(source) && !ignoreActionBlocker)
            return;

        var message = TransformSpeech(source, originalMessage);
        if (message.Length == 0)
            return;

        var speakerProcessedMessage = _language.ObfuscateMessageForSpeaker(source, message, language);
        var speech = GetSpeechVerb(source, speakerProcessedMessage);

        string name;
        if (nameOverride != null)
        {
            name = nameOverride;
        }
        else
        {
            var nameEv = new TransformSpeakerNameEvent(source, Name(source));
            RaiseLocalEvent(source, nameEv);
            name = nameEv.VoiceName;
            if (nameEv.SpeechVerb != null && _prototypeManager.TryIndex(nameEv.SpeechVerb, out var proto))
                speech = proto;
        }

        var transformedName = name;
        name = FormattedMessage.EscapeText(name);

        var languageTypeface = languagePrototype?.TypefaceId;
        var languageSize = languagePrototype?.TextSize;
        var showLanguageName = languagePrototype?.ShowLanguageName ?? false;
        var languageIcon = languagePrototype?.DisplayedLanguageIcon;

        var typefaceToUse = languageTypeface ?? speech.FontId;
        var sizeToUse = languageSize ?? speech.FontSize;

        var languageIndicator = string.Empty;
        if (showLanguageName && languagePrototype != null && string.IsNullOrEmpty(languageIcon))
            languageIndicator = $" ({languagePrototype.LocalizedName})";

        var wrappedMessageTemplate = Loc.GetString(
            speech.Bold ? "chat-manager-entity-say-bold-wrap-message" : "chat-manager-entity-say-wrap-message",
            ("entityName", "{1}" + languageIndicator),
            ("verb", Loc.GetString(_random.Pick(speech.SpeechVerbStrings))),
            ("fontType", typefaceToUse),
            ("fontSize", sizeToUse),
            ("message", "{0}"));

        SendInVoiceRangeWithLanguage(
            ChatChannel.Local,
            speakerProcessedMessage,
            wrappedMessageTemplate,
            source,
            range,
            language,
            languageIcon,
            speakerName: name,
            visibleLanguage: !(languagePrototype?.NeedsSpeech ?? true),
            transformedName: transformedName,
            needsLos: needsLos);

        var ev = new EntitySpokeEvent(source, speakerProcessedMessage, null, null, language);
        RaiseLocalEvent(source, ev, true);

        if (!HasComp<ActorComponent>(source) || hideLog)
            return;

        var logMessage = originalMessage == speakerProcessedMessage
            ? originalMessage
            : $"original: {originalMessage}, transformed: {speakerProcessedMessage}";
        var logName = name != Name(source) ? $" as {name}" : string.Empty;
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Say from {ToPrettyString(source):user}{logName} in {language}: {logMessage}.");
    }

    private void SendEntityWhisperWithLanguage(
        EntityUid source,
        string originalMessage,
        ChatTransmitRange range,
        RadioChannelPrototype? channel,
        string? nameOverride,
        bool hideLog,
        bool ignoreActionBlocker,
        ProtoId<LanguagePrototype> language,
        bool ignoreXenos = false)
    {
        LanguagePrototype? languagePrototype = null;
        if (!_prototypeManager.TryIndex(language, out languagePrototype))
        {
            language = SharedLanguageSystem.CommonLanguage;
            _prototypeManager.TryIndex(language, out languagePrototype);
        }

        var needsLos = languagePrototype?.NeedsLOS ?? false;
        var needsSpeech = languagePrototype?.NeedsSpeech ?? true;

        if (needsSpeech && !_actionBlocker.CanSpeak(source) && !ignoreActionBlocker)
            return;

        var message = TransformSpeech(source, FormattedMessage.RemoveMarkupOrThrow(originalMessage));
        if (message.Length == 0)
            return;

        var speakerMessage = _language.ObfuscateMessageForSpeaker(source, message, language);

        var transformedIdentityName = nameOverride ?? Identity.Name(source, EntityManager).Name;
        string name;
        if (nameOverride != null)
        {
            name = nameOverride;
        }
        else
        {
            var nameEv = new TransformSpeakerNameEvent(source, Name(source));
            RaiseLocalEvent(source, nameEv);
            name = nameEv.VoiceName;
        }

        var transformedName = name;
        name = FormattedMessage.EscapeText(name);

        var showLanguageName = languagePrototype?.ShowLanguageName ?? false;
        var languageIcon = showLanguageName ? languagePrototype?.DisplayedLanguageIcon : null;
        var visibleLanguage = !(languagePrototype?.NeedsSpeech ?? true);

        foreach (var (session, data) in GetRecipients(source, WhisperMuffledRange, ignoreXenos))
        {
            if (session.AttachedEntity is not { Valid: true } listener)
                continue;

            if (MessageRangeCheck(session, data, range) != MessageRangeCheckResult.Full)
                continue;

            if (needsLos && !data.Observer && listener != source && !data.HasLOS)
                continue;

            var listenerMessage = listener == source
                ? speakerMessage
                : _language.ObfuscateMessageForListener(listener, speakerMessage, language);
            var listenerName = FormattedMessage.EscapeText(GetSpeakerNameForListener(source, listener, transformedName));
            var listenerIdentityName = FormattedMessage.EscapeText(GetSpeakerNameForListener(source, listener, transformedIdentityName));

            string actualWrappedMessage;

            if (data.Range <= WhisperClearRange)
            {
                var hidePopup = visibleLanguage && !_language.CanUnderstand(listener, language);
                var useEmoteSpeechBubble = hidePopup;
                actualWrappedMessage = hidePopup
                    ? Loc.GetString(
                        "chat-manager-entity-me-wrap-message",
                        ("entityName", listenerName),
                        ("entity", source),
                        ("message", FormattedMessage.RemoveMarkupOrThrow(listenerMessage)))
                    : Loc.GetString(
                        "chat-manager-entity-whisper-wrap-message",
                        ("entityName", listenerName),
                        ("message", FormattedMessage.EscapeText(listenerMessage)));
                _chatManager.ChatMessageToOne(
                    ChatChannel.Whisper,
                    listenerMessage,
                    actualWrappedMessage,
                    source,
                    false,
                    session.Channel,
                    hidePopup: hidePopup,
                    useEmoteSpeechBubble: useEmoteSpeechBubble,
                    languageIcon: languageIcon);
            }
            else if (_examineSystem.InRangeUnOccluded(source, listener, WhisperMuffledRange))
            {
                var obfuscatedMessage = ObfuscateMessageReadability(listenerMessage, 0.2f);
                var hidePopup = visibleLanguage && !_language.CanUnderstand(listener, language);
                var useEmoteSpeechBubble = hidePopup;
                actualWrappedMessage = hidePopup
                    ? Loc.GetString(
                        "chat-manager-entity-me-wrap-message",
                        ("entityName", listenerIdentityName),
                        ("entity", source),
                        ("message", FormattedMessage.RemoveMarkupOrThrow(obfuscatedMessage)))
                    : Loc.GetString(
                        "chat-manager-entity-whisper-wrap-message",
                        ("entityName", listenerIdentityName),
                        ("message", FormattedMessage.EscapeText(obfuscatedMessage)));
                _chatManager.ChatMessageToOne(
                    ChatChannel.Whisper,
                    obfuscatedMessage,
                    actualWrappedMessage,
                    source,
                    false,
                    session.Channel,
                    hidePopup: hidePopup,
                    useEmoteSpeechBubble: useEmoteSpeechBubble,
                    languageIcon: languageIcon);
            }
            else
            {
                var obfuscatedMessage = ObfuscateMessageReadability(listenerMessage, 0.2f);
                actualWrappedMessage = Loc.GetString(
                    "chat-manager-entity-whisper-unknown-wrap-message",
                    ("message", FormattedMessage.EscapeText(obfuscatedMessage)));
                _chatManager.ChatMessageToOne(
                    ChatChannel.Whisper,
                    obfuscatedMessage,
                    actualWrappedMessage,
                    source,
                    false,
                    session.Channel,
                    languageIcon: languageIcon);
            }
        }

        var replayWrappedMessage = Loc.GetString(
            "chat-manager-entity-whisper-wrap-message",
            ("entityName", name),
            ("message", FormattedMessage.EscapeText(speakerMessage)));
        _replay.RecordServerMessage(
            new ChatMessage(
                ChatChannel.Whisper,
                speakerMessage,
                replayWrappedMessage,
                GetNetEntity(source),
                null,
                MessageRangeHideChatForReplay(range),
                speechStyleClass: CompOrNull<RMCSpeechBubbleSpecificStyleComponent>(source)?.SpeechStyleClass,
                repeatCheckSender: !HasComp<ChatRepeatIgnoreSenderComponent>(source),
                languageIcon: languageIcon));

        var muffledMessage = ObfuscateMessageReadability(speakerMessage, 0.2f);
        var ev = new EntitySpokeEvent(source, speakerMessage, channel, muffledMessage, language);
        RaiseLocalEvent(source, ev, true);

        if (hideLog)
            return;

        var logMessage = originalMessage == speakerMessage
            ? originalMessage
            : $"original: {originalMessage}, transformed: {speakerMessage}";
        var logName = name != Name(source) ? $" as {name}" : string.Empty;
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Whisper from {ToPrettyString(source):user}{logName} in {language}: {logMessage}.");
    }

    public void SendRadioSpeakerWhisperWithLanguage(
        EntityUid source,
        string message,
        ProtoId<LanguagePrototype> language,
        string? nameOverride = null,
        bool ignoreXenos = false,
        EntityUid? originalSpeaker = null)
    {
        LanguagePrototype? languagePrototype = null;
        if (!_prototypeManager.TryIndex(language, out languagePrototype))
        {
            language = SharedLanguageSystem.CommonLanguage;
            _prototypeManager.TryIndex(language, out languagePrototype);
        }

        var needsLos = languagePrototype?.NeedsLOS ?? false;
        var showLanguageName = languagePrototype?.ShowLanguageName ?? false;
        var languageIcon = showLanguageName ? languagePrototype?.DisplayedLanguageIcon : null;
        var visibleLanguage = !(languagePrototype?.NeedsSpeech ?? true);
        var transformedName = nameOverride ?? Identity.Name(source, EntityManager).Name;

        foreach (var (session, data) in GetRecipients(source, WhisperMuffledRange, ignoreXenos))
        {
            if (session.AttachedEntity is not { Valid: true } listener)
                continue;

            if (MessageRangeCheck(session, data, ChatTransmitRange.GhostRangeLimit) != MessageRangeCheckResult.Full)
                continue;

            if (needsLos && !data.Observer && listener != source && !data.HasLOS)
                continue;

            var listenerMessage = listener == source
                ? message
                : _language.ObfuscateMessageForListener(listener, message, language);
            var listenerName = FormattedMessage.EscapeText(GetRadioSpeakerNameForListener(source, listener, transformedName, originalSpeaker));

            string actualWrappedMessage;

            if (data.Range <= WhisperClearRange)
            {
                var hidePopup = visibleLanguage && !_language.CanUnderstand(listener, language);
                var useEmoteSpeechBubble = hidePopup;
                actualWrappedMessage = hidePopup
                    ? Loc.GetString(
                        "chat-manager-entity-me-wrap-message",
                        ("entityName", listenerName),
                        ("entity", source),
                        ("message", FormattedMessage.RemoveMarkupOrThrow(listenerMessage)))
                    : Loc.GetString(
                        "chat-manager-entity-whisper-wrap-message",
                        ("entityName", listenerName),
                        ("message", FormattedMessage.EscapeText(listenerMessage)));
                _chatManager.ChatMessageToOne(
                    ChatChannel.Whisper,
                    listenerMessage,
                    actualWrappedMessage,
                    source,
                    false,
                    session.Channel,
                    hidePopup: hidePopup,
                    useEmoteSpeechBubble: useEmoteSpeechBubble,
                    languageIcon: languageIcon);
            }
            else if (data.HasLOS)
            {
                var obfuscatedMessage = ObfuscateMessageReadability(listenerMessage, 0.2f);
                var hidePopup = visibleLanguage && !_language.CanUnderstand(listener, language);
                var useEmoteSpeechBubble = hidePopup;
                actualWrappedMessage = hidePopup
                    ? Loc.GetString(
                        "chat-manager-entity-me-wrap-message",
                        ("entityName", listenerName),
                        ("entity", source),
                        ("message", FormattedMessage.RemoveMarkupOrThrow(obfuscatedMessage)))
                    : Loc.GetString(
                        "chat-manager-entity-whisper-wrap-message",
                        ("entityName", listenerName),
                        ("message", FormattedMessage.EscapeText(obfuscatedMessage)));
                _chatManager.ChatMessageToOne(
                    ChatChannel.Whisper,
                    obfuscatedMessage,
                    actualWrappedMessage,
                    source,
                    false,
                    session.Channel,
                    hidePopup: hidePopup,
                    useEmoteSpeechBubble: useEmoteSpeechBubble,
                    languageIcon: languageIcon);
            }
            else
            {
                var obfuscatedMessage = ObfuscateMessageReadability(listenerMessage, 0.2f);
                actualWrappedMessage = Loc.GetString(
                    "chat-manager-entity-whisper-unknown-wrap-message",
                    ("message", FormattedMessage.EscapeText(obfuscatedMessage)));
                _chatManager.ChatMessageToOne(
                    ChatChannel.Whisper,
                    obfuscatedMessage,
                    actualWrappedMessage,
                    source,
                    false,
                    session.Channel,
                    languageIcon: languageIcon);
            }
        }

        var replayWrappedMessage = Loc.GetString(
            "chat-manager-entity-whisper-wrap-message",
            ("entityName", FormattedMessage.EscapeText(transformedName)),
            ("message", FormattedMessage.EscapeText(message)));
        _replay.RecordServerMessage(
            new ChatMessage(
                ChatChannel.Whisper,
                message,
                replayWrappedMessage,
                GetNetEntity(source),
                null,
                MessageRangeHideChatForReplay(ChatTransmitRange.GhostRangeLimit),
                speechStyleClass: CompOrNull<RMCSpeechBubbleSpecificStyleComponent>(source)?.SpeechStyleClass,
                repeatCheckSender: !HasComp<ChatRepeatIgnoreSenderComponent>(source),
                languageIcon: languageIcon));

        var muffledMessage = ObfuscateMessageReadability(message, 0.2f);
        var ev = new EntitySpokeEvent(source, message, null, muffledMessage, language);
        RaiseLocalEvent(source, ev, true);
    }

    private string GetRadioSpeakerNameForListener(EntityUid source, EntityUid listener, string transformedName, EntityUid? originalSpeaker)
    {
        if (originalSpeaker is not { } speaker)
            return GetSpeakerNameForListener(source, listener, transformedName);

        var originalSpeakerName = GetSpeakerNameForListener(speaker, listener, transformedName);
        return Loc.GetString("speech-name-relay",
            ("speaker", Identity.Name(source, EntityManager, listener)),
            ("originalName", originalSpeakerName));
    }

    private void SendInVoiceRangeWithLanguage(
        ChatChannel channel,
        string speakerMessage,
        string wrappedMessageTemplate,
        EntityUid source,
        ChatTransmitRange range,
        ProtoId<LanguagePrototype> language,
        string? languageIcon = null,
        string? speakerName = null,
        bool visibleLanguage = false,
        NetUserId? author = null,
        string? transformedName = null,
        bool needsLos = false)
    {
        foreach (var (session, data) in GetRecipients(source, VoiceRange))
        {
            var entRange = MessageRangeCheck(session, data, range);
            if (entRange == MessageRangeCheckResult.Disallowed)
                continue;

            var entHideChat = entRange == MessageRangeCheckResult.HideChat;

            if (session.AttachedEntity is not { Valid: true } listener)
                continue;

            if (needsLos && !data.Observer && listener != source && !data.HasLOS)
                continue;

            var canUnderstand = _language.CanUnderstand(listener, language);
            var listenerMessage = listener == source
                ? speakerMessage
                : _language.ObfuscateMessageForListener(listener, speakerMessage, language);
            var useEmoteSpeechBubble = visibleLanguage && !canUnderstand;
            var listenerName = FormattedMessage.EscapeText(GetSpeakerNameForListener(
                source,
                listener,
                transformedName ?? speakerName ?? Name(source)));

            var finalWrappedMessage = useEmoteSpeechBubble
                ? Loc.GetString(
                    "chat-manager-entity-me-wrap-message",
                    ("entityName", listenerName),
                    ("entity", source),
                    ("message", FormattedMessage.RemoveMarkupOrThrow(listenerMessage)))
                : string.Format(
                    wrappedMessageTemplate,
                    FormattedMessage.EscapeText(listenerMessage),
                    listenerName);

            var ev = new ChatMessageOverrideInVoiceRangeEvent(
                session,
                channel,
                source,
                listenerMessage,
                finalWrappedMessage,
                entHideChat);

            if (session.AttachedEntity != null)
                RaiseLocalEvent(session.AttachedEntity.Value, ref ev);
            else
                RaiseLocalEvent(source, ref ev);

            _chatManager.ChatMessageToOne(
                channel,
                ev.Message,
                ev.WrappedMessage,
                source,
                ev.EntHideChat,
                session.Channel,
                author: author,
                hidePopup: visibleLanguage && !canUnderstand,
                useEmoteSpeechBubble: useEmoteSpeechBubble,
                languageIcon: languageIcon);
        }

        var replayWrappedMessage = string.Format(
            wrappedMessageTemplate,
            FormattedMessage.EscapeText(speakerMessage),
            speakerName ?? FormattedMessage.EscapeText(Name(source)));
        _replay.RecordServerMessage(
            new ChatMessage(
                channel,
                speakerMessage,
                replayWrappedMessage,
                GetNetEntity(source),
                null,
                MessageRangeHideChatForReplay(range),
                speechStyleClass: CompOrNull<RMCSpeechBubbleSpecificStyleComponent>(source)?.SpeechStyleClass,
                repeatCheckSender: !HasComp<ChatRepeatIgnoreSenderComponent>(source),
                languageIcon: languageIcon));
    }
}
