using Content.Shared.Chat;
using Robust.Shared.Player;
using static Content.Server.Chat.Systems.ChatSystem;

namespace Content.Server._RMC14.Chat.Chat;

[ByRefEvent]
public record struct ChatMessageOverrideInVoiceRangeEvent(ICommonSession HearingSession, ChatChannel Channel, EntityUid Source, string Message, string WrappedMessage, bool EntHideChat);
