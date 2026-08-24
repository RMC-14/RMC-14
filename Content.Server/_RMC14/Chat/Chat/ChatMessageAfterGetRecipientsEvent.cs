using Content.Shared.Chat;
using Robust.Shared.Player;
using static Content.Server.Chat.Systems.ChatSystem;

namespace Content.Server._RMC14.Chat.Chat;

[ByRefEvent]
public readonly record struct ChatMessageAfterGetRecipientsEvent(Dictionary<ICommonSession, ICChatRecipientData> Recipients, ChatChannel Channel);
