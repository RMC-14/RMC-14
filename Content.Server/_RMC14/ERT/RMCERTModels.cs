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
    /// Unique runtime id used by admin UI and shuttle/member ownership.
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
    /// Display name for the request source shown to admins.
    /// </summary>
    public string SourceName = string.Empty;

    /// <summary>
    /// Display name for the requester shown to admins.
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
    /// Whether admins may approve this request by weighted-random selection.
    /// </summary>
    public bool AllowRandomSelection = true;

    /// <summary>
    /// Whether admins may approve this request by choosing a specific call.
    /// </summary>
    public bool AllowSpecificSelection = true;

    /// <summary>
    /// Last blocking error shown in admin UI.
    /// </summary>
    public string LastError = string.Empty;

    /// <summary>
    /// Last recoverable warning shown in admin UI, typically a preflight block that left the request pending.
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
    /// Materialized roster slots planned from the selected call prototype.
    /// </summary>
    public readonly List<RMCERTRosterSlot> PlannedRoster = [];

    /// <summary>
    /// Ghost-role entities spawned for recruitment and later cleanup.
    /// </summary>
    public readonly List<EntityUid> SpawnedGhostRoles = [];
}

/// <summary>
/// Arguments for creating an ERT request that waits for admin approval.
/// </summary>
public sealed class RMCERTCreateRequestArgs
{
    /// <summary>
    /// High-level channel that originated the request, such as console, handheld beacon, admin, or ARES automation.
    /// This is the source category, not a world entity or player.
    /// </summary>
    public RMCERTRequestSource Source = RMCERTRequestSource.Ares;

    /// <summary>
    /// Optional concrete world entity that originated the request, such as the communications console or handheld beacon.
    /// When set, ERT can reject duplicate non-terminal requests from the same source, record source cooldown timing,
    /// update source visuals, and show source popups.
    /// Leave null for source-less internal requests, for example ARES/event logic.
    /// </summary>
    public EntityUid? SourceEntity;

    /// <summary>
    /// Optional entity that initiated the request, usually the mob/player who used the source entity.
    /// This is separate from <see cref="SourceEntity"/>: a player can request through a console or beacon.
    /// Leave null when the request was created by server-side logic without a player actor.
    /// </summary>
    public EntityUid? Requester;

    /// <summary>
    /// Display/log fallback name for the source when <see cref="SourceEntity"/> is null or when the caller wants an explicit label.
    /// If empty and a source entity exists, the entity name is used; otherwise the localized <see cref="Source"/> label is used.
    /// </summary>
    public string SourceName = string.Empty;

    /// <summary>
    /// Display/log fallback name for the requester when <see cref="Requester"/> is null or when the caller wants an explicit label.
    /// If empty and a requester entity exists, the entity name is used; otherwise the localized <see cref="Source"/> label is used.
    /// </summary>
    public string RequesterName = string.Empty;

    /// <summary>
    /// Human-readable reason shown to admins, announcements, logs, and responder briefing where applicable.
    /// Validation of whether a reason is required is caller-specific and should be done before calling <c>CreateRequest</c>.
    /// </summary>
    public string Reason = string.Empty;

    /// <summary>
    /// Candidate set of ERT call prototypes this request may dispatch.
    /// The caller is responsible for preparing this list; use <c>GetCallOptions</c> first when source/enabled filtering is desired.
    /// A single entry means this request is constrained to one call.
    /// </summary>
    public List<ProtoId<RMCERTCallPrototype>> AllowedCalls = [];

    /// <summary>
    /// Whether admins may approve this request by weighted-random selection from enabled calls in <see cref="AllowedCalls"/>.
    /// Set true for random-only or mixed-choice requests; set false when the caller requires an explicit call choice.
    /// </summary>
    public bool AllowRandomSelection = true;

    /// <summary>
    /// Whether admins may approve this request by choosing a specific enabled call from <see cref="AllowedCalls"/>.
    /// Set true for specific-call or admin-choice requests; set false for random-only requests such as console distress.
    /// </summary>
    public bool AllowSpecificSelection = true;
}

/// <summary>
/// Arguments for immediately forcing a specific ERT call.
/// </summary>
public sealed class RMCERTForceCallArgs
{
    /// <summary>
    /// Specific ERT call prototype to create and approve immediately.
    /// The call must exist and be enabled; admin-facing adapters may add their own <c>AdminSelectable</c> restriction before calling.
    /// </summary>
    public ProtoId<RMCERTCallPrototype> Call;

    /// <summary>
    /// Optional entity performing the force call, usually the admin's attached entity.
    /// Used for logs and announcements when available.
    /// </summary>
    public EntityUid? Actor;

    /// <summary>
    /// Optional display/log name for <see cref="Actor"/>.
    /// Use this when the actor entity is absent or when the caller has a better actor label, such as an admin session name.
    /// </summary>
    public string? ActorName;

    /// <summary>
    /// Optional admin/system reason recorded on the created force-call request.
    /// </summary>
    public string Reason = string.Empty;
}

/// <summary>
/// Arguments for approving an ERT request.
/// </summary>
public sealed class RMCERTApproveRequestArgs
{
    /// <summary>
    /// Runtime id of the pending ERT request to approve.
    /// </summary>
    public Guid Request;

    /// <summary>
    /// Specific call to approve, or null to approve by weighted-random selection.
    /// Specific approval requires the request to allow specific selection and the call to be present in the request's allowed set.
    /// Random approval requires the request to allow random selection.
    /// </summary>
    public ProtoId<RMCERTCallPrototype>? Call;

    /// <summary>
    /// Optional entity approving the request, usually the admin's attached entity.
    /// Used for logs and announcements when available.
    /// </summary>
    public EntityUid? Actor;

    /// <summary>
    /// Optional display/log name for <see cref="Actor"/>.
    /// Use this when the actor entity is absent or when the caller has a better actor label, such as an admin session name.
    /// </summary>
    public string? ActorName;
}

/// <summary>
/// Arguments for acting on an existing ERT request.
/// </summary>
public sealed class RMCERTRequestActionArgs
{
    /// <summary>
    /// Runtime id of the ERT request to act on.
    /// </summary>
    public Guid Request;

    /// <summary>
    /// Optional entity performing the lifecycle action.
    /// Used for logs and announcements when available.
    /// </summary>
    public EntityUid? Actor;

    /// <summary>
    /// Optional display/log name for <see cref="Actor"/>.
    /// Use this when the actor entity is absent or when the caller has a better actor label, such as an admin session name.
    /// </summary>
    public string? ActorName;
}

/// <summary>
/// Arguments for querying ERT call options.
/// </summary>
public sealed class RMCERTCallQueryArgs
{
    /// <summary>
    /// When true, return only call prototypes with <see cref="RMCERTCallPrototype.Enabled"/> set.
    /// When false, include disabled calls as well.
    /// </summary>
    public bool EnabledOnly;

    /// <summary>
    /// When true, return only call prototypes marked as directly admin-selectable.
    /// This is used for force-call/default admin lists; request-specific approval can still allow other calls when they are
    /// explicitly present in the request's allowed set.
    /// </summary>
    public bool AdminSelectableOnly;

    /// <summary>
    /// Optional source filter.
    /// When set, only calls whose prototype <c>AllowedSources</c> contains this source are returned.
    /// Leave null to ignore source restrictions.
    /// </summary>
    public RMCERTRequestSource? Source;
}

/// <summary>
/// Public result returned by ERT API operations.
/// This is a safe operation receipt, not the internal mutable <see cref="RMCERTRequest"/> state object.
/// </summary>
/// <param name="Success">Whether the operation completed successfully.</param>
/// <param name="RequestId">Runtime id of the affected request, or <see cref="Guid.Empty"/> when no request was created/found.</param>
/// <param name="State">Current state of the affected request when known.</param>
/// <param name="Error">Terminal or blocking error message suitable for command/UI feedback. Empty on success or warning-only failures.</param>
/// <param name="Warning">Recoverable warning message, for example approval blocked by preflight conditions while the request remains pending.</param>
public readonly record struct RMCERTRequestResult(
    bool Success,
    Guid RequestId,
    RMCERTRequestState? State,
    string Error,
    string Warning);
