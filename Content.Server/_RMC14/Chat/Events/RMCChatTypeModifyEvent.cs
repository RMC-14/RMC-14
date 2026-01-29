using Content.Server.Chat.Systems;

namespace Content.Server._RMC14.Chat.Events;

/// <summary>
/// Event raised before sending an in-game IC message to allow systems to modify the chat type.
/// For example, converting Say to Whisper for entities that can only whisper.
/// </summary>
[ByRefEvent]
public record struct RMCChatTypeModifyEvent(
    EntityUid Source,
    string Message,
    InGameICChatType DesiredType
)
{
    /// <summary>
    /// The modified chat type. If set to a different value than DesiredType, the message will be sent as the new type.
    /// </summary>
    public InGameICChatType? ModifiedType { get; set; }
}
