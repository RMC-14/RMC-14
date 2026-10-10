using Content.Shared.Eui;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.GMRequest;

[Serializable, NetSerializable]
public record struct GMRequestLog(
    NetUserId Sender,
    string SenderName,
    string? EntityName,
    string Message,
    DateTime Time,
    string? ClaimName,
    bool Hidden
);

[Serializable, NetSerializable]
public sealed class GMRequestRequestLogs : EuiMessageBase;

[Serializable, NetSerializable]
public sealed class GMRequestClearMessage : EuiMessageBase;

[Serializable, NetSerializable]
public sealed class GMRequestSentLogsMessage : EuiMessageBase
{
    public Dictionary<int, GMRequestLog>? Logs;
}

[Serializable, NetSerializable]
public sealed class GMRequestSentLogMessage : EuiMessageBase
{
    public int Id;
    public GMRequestLog Log;
    public bool Sound;
}

[Serializable, NetSerializable]
public sealed class GMRequestClaimMessage : EuiMessageBase
{
    public string? Claimant;
    public int Id;
}

[Serializable, NetSerializable]
public sealed class GMRequestHideMessage : EuiMessageBase
{
    public int Id;
}

[Serializable, NetSerializable]
public sealed class GMRequestSubtleMessage : EuiMessageBase
{
    public NetUserId sender;
    public NetUserId target;
}
