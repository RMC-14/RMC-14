namespace Content.Shared._RMC14.Chat.Events;

/// <summary>
/// Event raised before sending an in-game IC message to allow systems to modify the chat type.
/// For example, converting Say to Whisper for entities that can only whisper.
/// </summary>
/// <remarks>
/// InGameICChatType is defined in Content.Server.Chat.Systems namespace.
/// Using byte to avoid dependency on server-side enum.
/// 0 = Speak, 1 = Emote, 2 = Whisper
/// </remarks>
[ByRefEvent]
public record struct RMCChatTypeModifyEvent(
    EntityUid Source,
    string Message,
    byte DesiredType
)
{
    /// <summary>
    /// The modified chat type. If set to a different value than DesiredType, the message will be sent as the new type.
    /// </summary>
    public byte? ModifiedType { get; set; }

    /// <summary>
    /// Whether to show a popup to the player about the type change.
    /// </summary>
    public bool ShowPopup { get; set; } = true;
}
