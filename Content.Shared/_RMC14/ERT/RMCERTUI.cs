using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.ERT;

[Serializable, NetSerializable]
public readonly record struct RMCERTCallOption(
    string Id,
    string Name,
    string Organization,
    string Category,
    int RandomWeight,
    bool AdminSelectable,
    string? AdminButtonLabel);

[Serializable, NetSerializable]
public readonly record struct RMCERTRequestOption(
    Guid Id,
    RMCERTRequestState State,
    RMCERTRequestSource Source,
    string SourceName,
    string RequesterName,
    string Reason,
    string? SelectedCall,
    List<string> AllowedCalls,
    string CreatedAt,
    string LastError);

[Serializable, NetSerializable]
public sealed class RMCERTAdminEuiState(
    List<RMCERTRequestOption> requests,
    List<RMCERTCallOption> calls
) : EuiStateBase
{
    public readonly List<RMCERTRequestOption> Requests = requests;
    public readonly List<RMCERTCallOption> Calls = calls;
}

[Serializable, NetSerializable]
public sealed class RMCERTAdminRefreshMsg : EuiMessageBase;

[Serializable, NetSerializable]
public sealed class RMCERTAdminApproveRandomMsg(Guid request) : EuiMessageBase
{
    public readonly Guid Request = request;
}

[Serializable, NetSerializable]
public sealed class RMCERTAdminApproveSpecificMsg(Guid request, string call) : EuiMessageBase
{
    public readonly Guid Request = request;
    public readonly string Call = call;
}

[Serializable, NetSerializable]
public sealed class RMCERTAdminDenyMsg(Guid request) : EuiMessageBase
{
    public readonly Guid Request = request;
}

[Serializable, NetSerializable]
public sealed class RMCERTAdminCancelMsg(Guid request) : EuiMessageBase
{
    public readonly Guid Request = request;
}

[Serializable, NetSerializable]
public sealed class RMCERTAdminLaunchMsg(Guid request) : EuiMessageBase
{
    public readonly Guid Request = request;
}

[Serializable, NetSerializable]
public sealed class RMCERTAdminCompleteMsg(Guid request) : EuiMessageBase
{
    public readonly Guid Request = request;
}
