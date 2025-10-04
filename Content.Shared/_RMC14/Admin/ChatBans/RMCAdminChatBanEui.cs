using Content.Shared.Database;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Admin.ChatBans;

[Serializable, NetSerializable]
public sealed class RMCAdminChatBanAddMsg(string target, ChatType type, TimeSpan duration, string reason) : EuiMessageBase
{
    public readonly string Target = target;
    public readonly ChatType Type = type;
    public readonly TimeSpan Duration = duration;
    public readonly string Reason = reason;
}

[Serializable, NetSerializable]
public sealed class RMCAdminChatBanAddErrorMsg(string message) : EuiMessageBase
{
    public readonly string Message = message;
}
