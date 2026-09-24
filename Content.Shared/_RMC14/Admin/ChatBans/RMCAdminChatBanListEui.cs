using Content.Shared.Database;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Admin.ChatBans;

[Serializable, NetSerializable]
public readonly record struct ChatBan(
    int Id,
    ChatType Type,
    DateTime BannedAt,
    DateTime? ExpiresAt,
    DateTime? UnbannedAt,
    string? UnbannedBy,
    string Reason
);

[Serializable, NetSerializable]
public sealed class RMCAdminChatBanListState(List<ChatBan> bans) : EuiStateBase
{
    public readonly List<ChatBan> Bans = bans;
}

[Serializable, NetSerializable]
public sealed class RMCAdminChatBanListPardonMsg(int id) : EuiMessageBase
{
    public readonly int Id = id;
}
