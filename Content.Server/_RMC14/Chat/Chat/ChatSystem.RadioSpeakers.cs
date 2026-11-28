using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared.Chat;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

// ReSharper disable CheckNamespace
namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    public void SendRadioSpeakersWhisperWithLanguage(
        IReadOnlyList<EntityUid> sources,
        string message,
        ProtoId<LanguagePrototype> language,
        string name,
        EntityUid originalSpeaker)
    {
        _prototypeManager.TryIndex(language, out LanguagePrototype? languagePrototype);
        var needsLos = languagePrototype?.NeedsLOS ?? false;
        var bestSpeakers = new Dictionary<ICommonSession, (EntityUid Source, ICChatRecipientData Data, int Clarity)>();

        foreach (var source in sources)
        {
            if (Deleted(source) || Terminating(source))
                continue;

            foreach (var (session, data) in GetRecipients(source, ChatChannel.Whisper, WhisperMuffledRange))
            {
                if (session.AttachedEntity is not { Valid: true } listener || data.Range < 0)
                    continue;

                var rangeCheck = MessageRangeCheck(session, data, ChatTransmitRange.GhostRangeLimit);
                if (rangeCheck != MessageRangeCheckResult.Full ||
                    needsLos && !data.Observer && listener != source && !data.HasLOS)
                {
                    continue;
                }

                var clarity = data.Range <= WhisperClearRange ? 2 : data.HasLOS ? 1 : 0;
                if (bestSpeakers.TryGetValue(session, out var best) &&
                    (clarity < best.Clarity ||
                     clarity == best.Clarity && (data.Range > best.Data.Range ||
                                                data.Range == best.Data.Range && source.Id >= best.Source.Id)))
                {
                    continue;
                }

                bestSpeakers[session] = (source, data, clarity);
            }
        }

        var recipientsBySpeaker = new Dictionary<EntityUid, Dictionary<ICommonSession, ICChatRecipientData>>();
        foreach (var (session, best) in bestSpeakers)
        {
            if (!recipientsBySpeaker.TryGetValue(best.Source, out var recipients))
            {
                recipients = new Dictionary<ICommonSession, ICChatRecipientData>();
                recipientsBySpeaker.Add(best.Source, recipients);
            }

            recipients.Add(session, best.Data);
        }

        var emptyRecipients = new Dictionary<ICommonSession, ICChatRecipientData>();
        foreach (var source in sources)
        {
            if (Deleted(source) || Terminating(source))
                continue;

            SendRadioSpeakerWhisperWithLanguage(
                source,
                message,
                language,
                name,
                originalSpeaker,
                recipientsBySpeaker.GetValueOrDefault(source) ?? emptyRecipients);
        }
    }
}
