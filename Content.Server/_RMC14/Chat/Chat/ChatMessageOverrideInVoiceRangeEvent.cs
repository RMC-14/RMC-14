using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared.Chat;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using static Content.Server.Chat.Systems.ChatSystem;

namespace Content.Server._RMC14.Chat.Chat;

[ByRefEvent]
public record struct ChatMessageOverrideInVoiceRangeEvent(
    ICommonSession HearingSession,
    ChatChannel Channel,
    EntityUid Source,
    string Message,
    string WrappedMessage,
    ProtoId<LanguagePrototype>? Language,
    bool EntHideChat);
