using System;
using Content.Shared._RMC14.ERT;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.ERT;

/// <summary>
/// Runtime state for a single ERT request from creation through cleanup.
/// </summary>
public sealed class RMCERTRequest
{
    /// <summary>
    /// Unique runtime id used by admin UI, Discord sync and shuttle/member ownership.
    /// </summary>
    public Guid Id = Guid.NewGuid();

    /// <summary>
    /// Current lifecycle state of the request.
    /// </summary>
    public RMCERTRequestState State = RMCERTRequestState.PendingAdmin;

    /// <summary>
    /// System or object type that created this request.
    /// </summary>
    public RMCERTRequestSource Source;

    /// <summary>
    /// Entity that originated the request, such as a communications console or handheld beacon.
    /// </summary>
    public EntityUid? SourceEntity;

    /// <summary>
    /// Player entity that submitted the request, when a player was involved.
    /// </summary>
    public EntityUid? Requester;

    /// <summary>
    /// Display name for the request source shown to admins and webhooks.
    /// </summary>
    public string SourceName = string.Empty;

    /// <summary>
    /// Display name for the requester shown to admins and webhooks.
    /// </summary>
    public string RequesterName = string.Empty;

    /// <summary>
    /// Player/admin supplied reason for requesting the response team.
    /// </summary>
    public string Reason = string.Empty;

    /// <summary>
    /// Round time when the request was created.
    /// </summary>
    public TimeSpan CreatedAt;

    /// <summary>
    /// Round time when dispatch was approved or scheduled.
    /// </summary>
    public TimeSpan? DispatchAt;

    /// <summary>
    /// Round time when ghost-role recruitment should close.
    /// </summary>
    public TimeSpan? RecruitmentEndsAt;

    /// <summary>
    /// Next round time when automatic launch may be attempted.
    /// </summary>
    public TimeSpan NextAutoLaunchAttempt;

    /// <summary>
    /// Call prototype selected for this request after approval.
    /// </summary>
    public ProtoId<RMCERTCallPrototype>? SelectedCall;

    /// <summary>
    /// Call prototypes this request is allowed to dispatch.
    /// </summary>
    public List<ProtoId<RMCERTCallPrototype>> AllowedCalls = [];

    /// <summary>
    /// Last blocking error shown in admin UI and webhook state.
    /// </summary>
    public string LastError = string.Empty;

    /// <summary>
    /// Last non-blocking warning shown in admin UI and webhook state.
    /// </summary>
    public string LastWarning = string.Empty;

    /// <summary>
    /// Shuttle grid currently assigned to this request.
    /// </summary>
    public EntityUid? Shuttle;

    /// <summary>
    /// Placed map marker or spawner used to stage the shuttle.
    /// </summary>
    public EntityUid? ShuttleSpawnMarker;

    /// <summary>
    /// Visual home coordinates used when reporting where the shuttle was staged.
    /// </summary>
    public MapCoordinates? ShuttleHomeVisualCoordinates;

    /// <summary>
    /// Coordinates used as the return route destination after arrival.
    /// </summary>
    public MapCoordinates? ShuttleHomeReturnCoordinates;

    /// <summary>
    /// Shuttle rotation to restore when generating a return destination.
    /// </summary>
    public Angle? ShuttleHomeRotation;

    /// <summary>
    /// Temporary return destination entity created for this shuttle.
    /// </summary>
    public EntityUid? ShuttleHomeDestination;

    /// <summary>
    /// Whether the shuttle was staged on the hidden fallback map instead of a physical start pad.
    /// </summary>
    public bool ShuttleHomeIsFallback;

    /// <summary>
    /// Round time when the shuttle may unlock its return route.
    /// </summary>
    public TimeSpan? ReturnAvailableAt;

    /// <summary>
    /// Whether the return destination has been made available to the shuttle crew.
    /// </summary>
    public bool ReturnRouteUnlocked;

    /// <summary>
    /// Materialized roster slots planned from the selected call prototype.
    /// </summary>
    public readonly List<RMCERTRosterSlot> PlannedRoster = [];

    /// <summary>
    /// Ghost-role entities spawned for recruitment and later cleanup.
    /// </summary>
    public readonly List<EntityUid> SpawnedGhostRoles = [];
}

[Serializable]
/// <summary>
/// Materialized roster entry created from a call prototype before ghost-role recruitment starts.
/// </summary>
public sealed class RMCERTRosterSlot
{
    /// <summary>
    /// Role id copied from the call prototype entry.
    /// </summary>
    public string RoleId = string.Empty;

    /// <summary>
    /// Localized role name shown to admins and responders.
    /// </summary>
    public string RoleName = string.Empty;

    /// <summary>
    /// Ghost-role entity prototype chosen for this slot.
    /// </summary>
    public EntProtoId GhostRoleEntity;

    /// <summary>
    /// Whether this slot represents the response team leader.
    /// </summary>
    public bool Leader;

    /// <summary>
    /// Higher-priority slots are spawned and seated first.
    /// </summary>
    public int Priority;

    /// <summary>
    /// Role tags used to choose spawn markers and reserved seats.
    /// </summary>
    public List<string> RoleTags = [];

    /// <summary>
    /// Preferred seat tags used when assigning this responder to the shuttle.
    /// </summary>
    public List<string> SeatTags = [];
}

[ByRefEvent]
/// <summary>
/// Raised when ERT request state changes and admin/webhook views need refreshing.
/// </summary>
public readonly record struct RMCERTStateChangedEvent;
