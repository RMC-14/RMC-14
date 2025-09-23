using Content.Shared.Chat;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Chat;

public sealed class RepeatedMessage(
    int index,
    FormattedMessage formattedMessage,
    NetEntity senderEntity,
    string message,
    ChatChannel channel
)
{
    public readonly int Index = index;
    public readonly FormattedMessage FormattedMessage = formattedMessage;
    public readonly NetEntity SenderEntity = senderEntity;
    public readonly string Message = message;
    public readonly ChatChannel Channel = channel;
    public int Count = 1;
}
