using Content.Shared.Chat;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Chat;

public sealed class RepeatedMessage(
    int index,
    FormattedMessage formattedMessage,
    NetEntity senderEntity,
    string message,
    ChatChannel channel,
    string? languageIcon
)
{
    public readonly int Index = index;
    public readonly FormattedMessage FormattedMessage = formattedMessage;
    public readonly NetEntity SenderEntity = senderEntity;
    public readonly string Message = message;
    public readonly ChatChannel Channel = channel;
    public readonly string? LanguageIcon = languageIcon;
    public int Count = 1;
    internal LanguageIconTag.LanguageIconControl? IconControl;
}
