using System;
using Content.Shared._RMC14.ERT;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.ERT;

/// <summary>
/// Runtime state for a single ERT request from creation through cleanup.
/// </summary>
public sealed class RMCERTRequest
{
    public Guid Id = Guid.NewGuid();
    public RMCERTRequestState State = RMCERTRequestState.PendingAdmin;
    public RMCERTRequestSource Source;
    public EntityUid? SourceEntity;
    public EntityUid? Requester;
    public string SourceName = string.Empty;
    public string RequesterName = string.Empty;
    public string Reason = string.Empty;
    public TimeSpan CreatedAt;
    public TimeSpan? DispatchAt;
    public TimeSpan? RecruitmentEndsAt;
    public TimeSpan NextAutoLaunchAttempt;
    public ProtoId<RMCERTCallPrototype>? SelectedCall;
    public List<ProtoId<RMCERTCallPrototype>> AllowedCalls = [];
    public string LastError = string.Empty;
    public string LastWarning = string.Empty;
    public EntityUid? Shuttle;
    public EntityUid? ShuttleSpawnMarker;
    public EntityUid? ShuttleHomeDestination;
    public bool ShuttleHomeIsFallback;
    public TimeSpan? ReturnAvailableAt;
    public bool ReturnRouteUnlocked;
    public readonly List<RMCERTRosterSlot> PlannedRoster = [];
    public readonly List<EntityUid> SpawnedGhostRoles = [];
}

[Serializable]
/// <summary>
/// Materialized roster entry created from a call prototype before ghost-role recruitment starts.
/// </summary>
public sealed class RMCERTRosterSlot
{
    public string RoleId = string.Empty;
    public string RoleName = string.Empty;
    public EntProtoId GhostRoleEntity;
    public bool Leader;
    public int Priority;
    public List<string> RoleTags = [];
    public List<string> SeatTags = [];
}

[ByRefEvent]
public readonly record struct RMCERTStateChangedEvent;
