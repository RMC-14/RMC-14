using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.ERT;

/// <summary>
/// Slimmed-down call data sent to the admin EUI.
/// </summary>
/// <param name="Id">Prototype id of the ERT call.</param>
/// <param name="Name">Localized call name shown in admin controls.</param>
/// <param name="Organization">Localized organization label for grouping and context.</param>
/// <param name="Category">Localized admin UI category label.</param>
/// <param name="RandomWeight">Weighted-random selection weight for random approval.</param>
/// <param name="AdminSelectable">
/// Whether the prototype is exposed in default direct-admin lists such as force-call controls.
/// Request-specific approval may still expose other calls when they are explicitly allowed by the request.
/// </param>
/// <param name="AdminButtonLabel">Optional localized force-call button label.</param>
[Serializable, NetSerializable]
public readonly record struct RMCERTCallOption(
    string Id,
    string Name,
    string Organization,
    string Category,
    int RandomWeight,
    bool AdminSelectable,
    string? AdminButtonLabel);

/// <summary>
/// Slimmed-down request data sent to the admin EUI.
/// </summary>
/// <param name="Id">Runtime request id used by admin action messages.</param>
/// <param name="State">Current lifecycle state of the request.</param>
/// <param name="Source">Origin type that created the request.</param>
/// <param name="SourceName">Display name for the request source.</param>
/// <param name="RequesterName">Display name for the requester.</param>
/// <param name="Reason">Reason supplied when the request was created.</param>
/// <param name="SelectedCall">Display label for the call selected for dispatch, if any.</param>
/// <param name="AllowedCalls">Call ids that this request may dispatch.</param>
/// <param name="AllowRandomSelection">Whether admins may approve this request by weighted-random selection.</param>
/// <param name="AllowSpecificSelection">Whether admins may approve this request by choosing a specific call.</param>
/// <param name="CreatedAt">Formatted round time when the request was created.</param>
/// <param name="LastError">Last blocking error for this request.</param>
/// <param name="LastWarning">Last recoverable warning for this request, typically a preflight block that left it pending.</param>
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
    bool AllowRandomSelection,
    bool AllowSpecificSelection,
    string CreatedAt,
    string LastError,
    string LastWarning);

/// <summary>
/// Current state of the admin ERT dispatch window.
/// </summary>
/// <param name="requests">Requests currently visible in the admin dispatch window.</param>
/// <param name="calls">Call options available for request approval.</param>
/// <param name="canForceCalls">Whether the viewer can send force-call messages.</param>
/// <param name="forceCalls">Call options exposed in the force-call tab.</param>
[Serializable, NetSerializable]
public sealed class RMCERTAdminEuiState(
    List<RMCERTRequestOption> requests,
    List<RMCERTCallOption> calls,
    bool canForceCalls,
    List<RMCERTCallOption> forceCalls
) : EuiStateBase
{
    /// <summary>
    /// Requests currently visible in the admin dispatch window.
    /// </summary>
    public readonly List<RMCERTRequestOption> Requests = requests;

    /// <summary>
    /// Call options available for approving requests.
    /// </summary>
    public readonly List<RMCERTCallOption> Calls = calls;

    /// <summary>
    /// Whether the viewer may dispatch force calls without a player request.
    /// </summary>
    public readonly bool CanForceCalls = canForceCalls;

    /// <summary>
    /// Call options exposed in the force-call tab.
    /// </summary>
    public readonly List<RMCERTCallOption> ForceCalls = forceCalls;
}

/// <summary>
/// Requests a fresh admin EUI state from the server.
/// </summary>
[Serializable, NetSerializable]
public sealed class RMCERTAdminRefreshMsg : EuiMessageBase;

/// <summary>
/// Requests a weighted-random call selection for the target request.
/// </summary>
/// <param name="request">Runtime request id to approve.</param>
[Serializable, NetSerializable]
public sealed class RMCERTAdminApproveRandomMsg(Guid request) : EuiMessageBase
{
    /// <summary>
    /// Runtime request id to approve.
    /// </summary>
    public readonly Guid Request = request;
}

/// <summary>
/// Requests a specific call selection for the target request.
/// </summary>
/// <param name="request">Runtime request id to approve.</param>
/// <param name="call">Prototype id of the call to dispatch.</param>
[Serializable, NetSerializable]
public sealed class RMCERTAdminApproveSpecificMsg(Guid request, string call) : EuiMessageBase
{
    /// <summary>
    /// Runtime request id to approve.
    /// </summary>
    public readonly Guid Request = request;

    /// <summary>
    /// Prototype id of the call to dispatch.
    /// </summary>
    public readonly string Call = call;
}

/// <summary>
/// Requests denial of a pending ERT request.
/// </summary>
/// <param name="request">Runtime request id to deny.</param>
[Serializable, NetSerializable]
public sealed class RMCERTAdminDenyMsg(Guid request) : EuiMessageBase
{
    /// <summary>
    /// Runtime request id to deny.
    /// </summary>
    public readonly Guid Request = request;
}

/// <summary>
/// Requests cancellation of an approved or active ERT request.
/// </summary>
/// <param name="request">Runtime request id to cancel.</param>
[Serializable, NetSerializable]
public sealed class RMCERTAdminCancelMsg(Guid request) : EuiMessageBase
{
    /// <summary>
    /// Runtime request id to cancel.
    /// </summary>
    public readonly Guid Request = request;
}

/// <summary>
/// Requests immediate launch of a recruiting ERT request.
/// </summary>
/// <param name="request">Runtime request id to launch.</param>
[Serializable, NetSerializable]
public sealed class RMCERTAdminLaunchMsg(Guid request) : EuiMessageBase
{
    /// <summary>
    /// Runtime request id to launch.
    /// </summary>
    public readonly Guid Request = request;
}

/// <summary>
/// Requests an admin-forced call without a player distress source.
/// </summary>
/// <param name="call">Prototype id of the call to force dispatch.</param>
/// <param name="reason">Admin supplied reason for the force call.</param>
[Serializable, NetSerializable]
public sealed class RMCERTAdminForceCallMsg(string call, string reason) : EuiMessageBase
{
    /// <summary>
    /// Prototype id of the call to force dispatch.
    /// </summary>
    public readonly string Call = call;

    /// <summary>
    /// Admin supplied reason for the force call.
    /// </summary>
    public readonly string Reason = reason;
}
