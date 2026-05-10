using Content.Server._RMC14.Chat.Chat;
using Content.Shared._RMC14.Chat;
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

    private void SendEntitySpeakWithLanguage(
        EntityUid source,
        string originalMessage,
        ChatTransmitRange range,
        string? nameOverride,
        bool hideLog,
        bool ignoreActionBlocker,
        ProtoId<LanguagePrototype> language)
    {
        if (!_actionBlocker.CanSpeak(source) && !ignoreActionBlocker)
            return;

        var message = TransformSpeech(source, originalMessage);
        if (message.Length == 0)
            return;

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
            ("entityName", name + languageIndicator),
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
        if (!_actionBlocker.CanSpeak(source) && !ignoreActionBlocker)
            return;

        var message = TransformSpeech(source, FormattedMessage.RemoveMarkupOrThrow(originalMessage));
        if (message.Length == 0)
            return;

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

        var speakerMessage = _language.ObfuscateMessageForSpeaker(source, message, language);

        var nameIdentity = FormattedMessage.EscapeText(nameOverride ?? Identity.Name(source, EntityManager));
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

            string actualWrappedMessage;

            if (data.Range <= WhisperClearRange)
            {
                var hidePopup = visibleLanguage && !_language.CanUnderstand(listener, language);
                actualWrappedMessage = hidePopup
                    ? Loc.GetString(
                        "chat-manager-entity-me-wrap-message",
                        ("entityName", name),
                        ("entity", source),
                        ("message", FormattedMessage.RemoveMarkupOrThrow(listenerMessage)))
                    : Loc.GetString(
                        "chat-manager-entity-whisper-wrap-message",
                        ("entityName", name),
                        ("message", FormattedMessage.EscapeText(listenerMessage)));
                _chatManager.ChatMessageToOne(
                    ChatChannel.Whisper,
                    listenerMessage,
                    actualWrappedMessage,
                    source,
                    false,
                    session.Channel,
                    hidePopup: hidePopup,
                    languageIcon: languageIcon);
            }
            else if (_examineSystem.InRangeUnOccluded(source, listener, WhisperMuffledRange))
            {
                var obfuscatedMessage = ObfuscateMessageReadability(listenerMessage, 0.2f);
                var hidePopup = visibleLanguage && !_language.CanUnderstand(listener, language);
                actualWrappedMessage = hidePopup
                    ? Loc.GetString(
                        "chat-manager-entity-me-wrap-message",
                        ("entityName", nameIdentity),
                        ("entity", source),
                        ("message", FormattedMessage.RemoveMarkupOrThrow(obfuscatedMessage)))
                    : Loc.GetString(
                        "chat-manager-entity-whisper-wrap-message",
                        ("entityName", nameIdentity),
                        ("message", FormattedMessage.EscapeText(obfuscatedMessage)));
                _chatManager.ChatMessageToOne(
                    ChatChannel.Whisper,
                    obfuscatedMessage,
                    actualWrappedMessage,
                    source,
                    false,
                    session.Channel,
                    hidePopup: hidePopup,
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

        var ev = new EntitySpokeEvent(source, speakerMessage, channel, null, language);
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
        bool ignoreXenos = false)
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
        var escapedName = FormattedMessage.EscapeText(nameOverride ?? Identity.Name(source, EntityManager));

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

            string actualWrappedMessage;

            if (data.Range <= WhisperClearRange)
            {
                var hidePopup = visibleLanguage && !_language.CanUnderstand(listener, language);
                actualWrappedMessage = hidePopup
                    ? Loc.GetString(
                        "chat-manager-entity-me-wrap-message",
                        ("entityName", escapedName),
                        ("entity", source),
                        ("message", FormattedMessage.RemoveMarkupOrThrow(listenerMessage)))
                    : Loc.GetString(
                        "chat-manager-entity-whisper-wrap-message",
                        ("entityName", escapedName),
                        ("message", FormattedMessage.EscapeText(listenerMessage)));
                _chatManager.ChatMessageToOne(
                    ChatChannel.Whisper,
                    listenerMessage,
                    actualWrappedMessage,
                    source,
                    false,
                    session.Channel,
                    hidePopup: hidePopup,
                    languageIcon: languageIcon);
            }
            else if (data.HasLOS)
            {
                var obfuscatedMessage = ObfuscateMessageReadability(listenerMessage, 0.2f);
                var hidePopup = visibleLanguage && !_language.CanUnderstand(listener, language);
                actualWrappedMessage = hidePopup
                    ? Loc.GetString(
                        "chat-manager-entity-me-wrap-message",
                        ("entityName", escapedName),
                        ("entity", source),
                        ("message", FormattedMessage.RemoveMarkupOrThrow(obfuscatedMessage)))
                    : Loc.GetString(
                        "chat-manager-entity-whisper-wrap-message",
                        ("entityName", escapedName),
                        ("message", FormattedMessage.EscapeText(obfuscatedMessage)));
                _chatManager.ChatMessageToOne(
                    ChatChannel.Whisper,
                    obfuscatedMessage,
                    actualWrappedMessage,
                    source,
                    false,
                    session.Channel,
                    hidePopup: hidePopup,
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
            ("entityName", escapedName),
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

        var ev = new EntitySpokeEvent(source, message, null, null, language);
        RaiseLocalEvent(source, ev, true);
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

            var finalWrappedMessage = visibleLanguage && !canUnderstand
                ? Loc.GetString(
                    "chat-manager-entity-me-wrap-message",
                    ("entityName", speakerName ?? FormattedMessage.EscapeText(Name(source))),
                    ("entity", source),
                    ("message", FormattedMessage.RemoveMarkupOrThrow(listenerMessage)))
                : string.Format(
                    wrappedMessageTemplate,
                    FormattedMessage.EscapeText(listenerMessage));

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
                languageIcon: languageIcon);
        }

        var replayWrappedMessage = string.Format(
            wrappedMessageTemplate,
            FormattedMessage.EscapeText(speakerMessage));
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
