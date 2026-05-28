using System.Linq;
using System.Numerics;
using System.Text;
using Content.Server._RMC14.Dropship;
using Content.Server._RMC14.Rules.DistressSignal;
using Content.Server._RMC14.Marines;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Humanoid.Components;
using Content.Server.Humanoid.Systems;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.ERT;
using Content.Shared._RMC14.Evacuation;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Rules;
using Content.Shared.Buckle;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.GameTicking;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Prototypes;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.ERT;

public sealed partial class RMCERTSystem
{

    /// <summary>
    /// Creates a new pending ERT request for admin review.
    /// The caller supplies the already-prepared allowed call set and approval modes; this method materializes the internal request,
    /// checks for duplicate non-terminal requests from the same source entity when one exists, records source cooldown timing,
    /// announces the pending request, and returns its id.
    /// </summary>
    /// <remarks>
    /// This method intentionally does not re-check prototype <c>AllowedSources</c>.
    /// Callers that want source filtering should call <see cref="GetCallOptions"/> first and pass the resulting ids through
    /// <see cref="RMCERTCreateRequestArgs.AllowedCalls"/>.
    /// </remarks>
    /// <param name="args">Creation parameters prepared by the caller.</param>
    /// <returns>
    /// Success with the new request id and <see cref="RMCERTRequestState.PendingAdmin"/>, or failure with an error when the request
    /// cannot be created.
    /// </returns>
    public RMCERTRequestResult CreateRequest(RMCERTCreateRequestArgs args)
    {
        var allowedCalls = args.AllowedCalls.Distinct().ToList();
        if (args.SourceEntity is { Valid: true } sourceEntity &&
            !CanCreateRequest(sourceEntity, out var error))
        {
            return ResultFailure(Guid.Empty, null, error);
        }

        if (allowedCalls.Count == 0)
        {
            return ResultFailure(Guid.Empty, null, Loc.GetString("rmc-ert-popup-no-source-teams"));
        }

        var reason = args.Reason.Trim();
        var autoResolveAt = GetAutoResolveAt(args.AutoResolution);
        var autoResolveActorName = GetAutoResolveActorName(args.AutoResolution);
        var request = new RMCERTRequest
        {
            Source = args.Source,
            SourceEntity = args.SourceEntity,
            Requester = args.Requester,
            SourceName = GetRequestSourceName(args.Source, args.SourceEntity, args.SourceName),
            RequesterName = GetRequestRequesterName(args.Source, args.Requester, args.RequesterName),
            Reason = reason,
            CreatedAt = _timing.CurTime,
            AllowedCalls = allowedCalls,
            AllowRandomSelection = args.AllowRandomSelection,
            AllowSpecificSelection = args.AllowSpecificSelection,
            AutoResolveAt = autoResolveAt,
            AutoApproveChance = ClampProbability(args.AutoResolution?.ApprovalChance ?? 0f),
            AutoResolveActorName = autoResolveActorName,
        };

        _requests[request.Id] = request;

        if (request.SourceEntity is { Valid: true } cooldownSource)
            _sourceCooldowns[cooldownSource] = _timing.CurTime;

        AddERTRequestLog(LogImpact.Medium,
            "created",
            request,
            GetRequesterLogText(request),
            extra: $"allowedCalls=[{string.Join(", ", allowedCalls.Select(c => c.Id))}], allowRandom={request.AllowRandomSelection}, allowSpecific={request.AllowSpecificSelection}");

        var text = BuildAdminRequestAnnouncement(request);
        _chat.SendAdminAnnouncement(text);
        AnnounceDistressLaunched(request);

        NotifyAdminsOfRequest();

        if (request.Source == RMCERTRequestSource.Handheld)
            UpdateSourceVisual(request, true);

        if (request.SourceEntity is { Valid: true } popupSource &&
            request.Requester is { Valid: true } popupRequester)
        {
            _popup.PopupEntity(GetRequestSuccessText(popupSource, request.Source), popupSource, popupRequester, PopupType.Medium);
        }

        DirtyState(request);
        return ResultSuccess(request);
    }

    /// <summary>
    /// Creates an admin-sourced request for one specific call and immediately approves it.
    /// This preserves force-call behavior while exposing the operation through the same public result contract as the normal lifecycle API.
    /// </summary>
    /// <param name="args">Force-call parameters, including the exact call id and optional actor/reason metadata.</param>
    /// <returns>
    /// Success with the created request id after approval, or failure when the call is unknown, disabled, or blocked during
    /// approval/preflight.
    /// </returns>
    public RMCERTRequestResult ForceCall(RMCERTForceCallArgs args)
    {
        var callId = args.Call;
        var reason = args.Reason.Trim();
        var actorText = GetAdminActorText(args.Actor, args.ActorName);

        if (!_prototypes.TryIndex(callId, out var call, false))
        {
            var unknownCallError = Loc.GetString("rmc-ert-error-unknown-call", ("id", callId.Id));
            _adminLog.Add(LogType.RMCAdminCommandLogging,
                LogImpact.Medium,
                $"ERT force call failed: actor={FormatLogValue(actorText)}, call={FormatLogValue(callId.Id)}, reason=\"{FormatLogValue(reason)}\", error=\"{FormatLogValue(unknownCallError)}\"");
            return ResultFailure(Guid.Empty, null, unknownCallError);
        }

        if (!call.Enabled)
        {
            var notForceCallableError = Loc.GetString("rmc-ert-error-call-disabled", ("call", call.Name));
            _adminLog.Add(LogType.RMCAdminCommandLogging,
                LogImpact.Medium,
                $"ERT force call failed: actor={FormatLogValue(actorText)}, call={FormatLogValue(call.Name)}, prototype={call.ID}, reason=\"{FormatLogValue(reason)}\", error=\"{FormatLogValue(notForceCallableError)}\"");
            return ResultFailure(Guid.Empty, null, notForceCallableError);
        }

        var request = new RMCERTRequest
        {
            Source = RMCERTRequestSource.Admin,
            Requester = args.Actor,
            SourceName = Loc.GetString("rmc-ert-source-admin"),
            RequesterName = actorText,
            Reason = reason,
            CreatedAt = _timing.CurTime,
            AllowedCalls = [callId],
            AllowRandomSelection = false,
            AllowSpecificSelection = true,
        };

        _requests[request.Id] = request;
        AddERTRequestLog(LogImpact.High, "force requested", request, request.RequesterName, call);

        var result = ApproveSpecificRequest(request.Id, callId, args.Actor, args.ActorName, false, true);
        if (result.Success)
            return result;

        var error = !string.IsNullOrWhiteSpace(request.LastError)
            ? request.LastError
            : !string.IsNullOrWhiteSpace(request.LastWarning)
                ? request.LastWarning
                : Loc.GetString("rmc-ert-error-force-call-failed", ("call", call.Name));
        return ResultFailure(request.Id, request.State, error, request.LastWarning);
    }

    /// <summary>
    /// Approves a pending ERT request either by weighted-random selection or by a specific call id.
    /// Passing a null call uses random selection from the request's allowed call set; passing a call id approves that exact call.
    /// </summary>
    /// <param name="args">Approval parameters with request id, optional selected call, and optional actor metadata.</param>
    /// <returns>
    /// Success when the request enters dispatch preparation, or failure when the request is not pending, the requested approval mode
    /// is disabled, the call is invalid/not allowed/disabled, requirements fail, or preflight blocks dispatch.
    /// </returns>
    public RMCERTRequestResult ApproveRequest(RMCERTApproveRequestArgs args)
    {
        if (!TryGetPending(args.Request, out var request))
            return RequestUnavailable(args.Request);

        if (args.Call is { } callId)
        {
            if (!request.AllowSpecificSelection)
            {
                request.LastError = Loc.GetString("rmc-ert-error-specific-selection-disabled");
                request.LastWarning = string.Empty;
                DirtyState(request);
                return ResultFailure(request.Id, request.State, request.LastError);
            }

            return ApproveSpecificRequest(args.Request, callId, args.Actor, args.ActorName, false, false);
        }

        if (!request.AllowRandomSelection)
        {
            request.LastError = Loc.GetString("rmc-ert-error-no-random-calls");
            request.LastWarning = string.Empty;
            DirtyState(request);
            return ResultFailure(request.Id, request.State, request.LastError);
        }

        if (!TryPickRandomCall(request, out var call, out var error))
        {
            FailRequest(request, error);
            return ResultFailure(request.Id, request.State, error);
        }

        return call is { } selected
            ? ApproveSpecificRequest(args.Request, selected, args.Actor, args.ActorName, true, false)
            : ResultFailure(request.Id, request.State, Loc.GetString("rmc-ert-error-random-selection-failed"));
    }

    private RMCERTRequestResult ApproveSpecificRequest(
        Guid id,
        ProtoId<RMCERTCallPrototype> callId,
        EntityUid? admin,
        string? adminName,
        bool randomSelection,
        bool forced = false)
    {
        if (!TryGetPending(id, out var request))
            return RequestUnavailable(id);

        if (!_prototypes.TryIndex(callId, out var call, false))
        {
            var unknownCallError = Loc.GetString("rmc-ert-error-unknown-call", ("id", callId.Id));
            FailRequest(request, unknownCallError);
            return ResultFailure(request.Id, request.State, unknownCallError);
        }

        if (!request.AllowedCalls.Contains(callId))
        {
            var notAllowedError = Loc.GetString("rmc-ert-error-call-not-allowed", ("call", call.Name));
            FailRequest(request, notAllowedError);
            return ResultFailure(request.Id, request.State, notAllowedError);
        }

        if (!call.Enabled)
        {
            var disabledError = Loc.GetString("rmc-ert-error-call-disabled", ("call", call.Name));
            FailRequest(request, disabledError);
            return ResultFailure(request.Id, request.State, disabledError);
        }

        if (!CheckRequirements(request, call, out var error))
        {
            FailRequest(request, error);
            return ResultFailure(request.Id, request.State, error);
        }

        if (!TryPrepareRequestForDispatch(request, call, true, out error))
        {
            request.LastError = string.Empty;
            request.LastWarning = error;
            AddERTRequestLog(LogImpact.Medium,
                forced ? "force approve blocked" : "approve blocked",
                request,
                GetAdminActorText(admin, adminName),
                call,
                $"warning=\"{FormatLogValue(error)}\"");
            DirtyState(request);
            return ResultFailure(request.Id, request.State, string.Empty, error);
        }

        request.State = RMCERTRequestState.PendingDispatch;
        request.SelectedCall = callId;
        request.LastError = string.Empty;
        request.LastWarning = string.Empty;

        var adminText = GetAdminActorText(admin, adminName);
        Log.Info($"ERT request {request.Id} approved as {call.ID} by {adminText}");
        AddERTRequestLog(forced ? LogImpact.High : LogImpact.Medium,
            forced ? "force approved" : "approved",
            request,
            adminText,
            call,
            $"selection={(randomSelection ? "random" : "specific")}, dispatchDelay={call.LaunchDelay:0}s");

        if (request.Source == RMCERTRequestSource.Admin)
            AnnounceDistressLaunched(request);

        DirtyState(request);
        Timer.Spawn(TimeSpan.FromSeconds(call.LaunchDelay), () => Dispatch(request.Id));
        return ResultSuccess(request);
    }

    /// <summary>
    /// Moves a non-terminal ERT request to denied, primarily for pending admin-review rejection.
    /// Denial runs source cleanup such as handheld beacon reset behavior, but does not clean up already-spawned ERT content;
    /// use <see cref="CancelRequest"/> for active requests that need content cleanup.
    /// </summary>
    /// <param name="args">Request id and optional actor metadata for logs/announcements.</param>
    /// <returns>Success when the request is moved to denied, or failure when the request is unknown or already terminal.</returns>
    public RMCERTRequestResult DenyRequest(RMCERTRequestActionArgs args)
    {
        if (!_requests.TryGetValue(args.Request, out var request))
            return RequestUnavailable(args.Request);

        if (IsTerminal(request.State))
            return RequestTerminal(request);

        request.State = RMCERTRequestState.Denied;
        request.LastError = string.Empty;
        request.LastWarning = string.Empty;

        if (request.SourceEntity is { Valid: true } beacon &&
            TryComp(beacon, out RMCERTDistressBeaconComponent? beaconComp) &&
            beaconComp.ResetOnDeny)
        {
            beaconComp.Spent = false;
        }

        UpdateSourceVisual(request, false);

        var adminText = GetAdminActorText(args.Actor, args.ActorName);
        AddERTRequestLog(LogImpact.Medium, "denied", request, adminText);

        if (request.SelectedCall is { } callId && _prototypes.TryIndex(callId, out var call))
            AnnounceNoResponse(request, call);
        else
            AnnounceNoResponse(request, null);

        DirtyState(request);
        return ResultSuccess(request);
    }

    /// <summary>
    /// Cancels a non-terminal ERT request by id and performs active content cleanup.
    /// Use this for already-created requests that should stop regardless of whether they are pending, recruiting, launching, or arrived.
    /// </summary>
    /// <param name="args">Request id and optional actor metadata for logs/announcements.</param>
    /// <returns>Success when the request is moved to cancelled, or failure when the request is unknown or already terminal.</returns>
    public RMCERTRequestResult CancelRequest(RMCERTRequestActionArgs args)
    {
        if (!_requests.TryGetValue(args.Request, out var request))
            return RequestUnavailable(args.Request);

        if (IsTerminal(request.State))
            return RequestTerminal(request);

        request.State = RMCERTRequestState.Cancelled;
        request.LastError = string.Empty;
        request.LastWarning = string.Empty;
        request.RecruitmentEndsAt = null;
        CleanupRequestContent(request);
        UpdateSourceVisual(request, false);
        var adminText = GetAdminActorText(args.Actor, args.ActorName);
        AddERTRequestLog(LogImpact.High, "cancelled", request, adminText);

        if (request.SelectedCall is { } callId && _prototypes.TryIndex(callId, out var call))
            Announce(call.Announcements.Cancelled, request, call);

        DirtyState(request);
        return ResultSuccess(request);
    }

    /// <summary>
    /// Manually launches an ERT request that is in the recruiting state.
    /// If recruitment is still open, this shortens the recruitment window before checking active ghost-role raffles and launch readiness.
    /// </summary>
    /// <param name="args">Request id and optional actor metadata for logs/announcements.</param>
    /// <returns>
    /// Success when launch begins, or failure when the request is unknown, not launchable, still has active raffles, or launch preflight fails.
    /// </returns>
    public RMCERTRequestResult LaunchRequest(RMCERTRequestActionArgs args)
    {
        var success = TryLaunch(args.Request, args.Actor, args.ActorName, false);
        if (!_requests.TryGetValue(args.Request, out var request))
            return RequestUnavailable(args.Request);

        return success
            ? ResultSuccess(request)
            : ResultFailure(request.Id, request.State, request.LastError, request.LastWarning);
    }

    /// <summary>
    /// Marks an arrived ERT request as completed and clears source visual state.
    /// This is normally called by shuttle/FTL flow when the team is done and the shuttle starts returning home.
    /// </summary>
    /// <param name="args">Request id and optional actor metadata for logs/announcements.</param>
    /// <returns>Success when the arrived request is completed, or failure when the request is unknown or not in the arrived state.</returns>
    public RMCERTRequestResult CompleteRequest(RMCERTRequestActionArgs args)
    {
        if (!_requests.TryGetValue(args.Request, out var request))
            return RequestUnavailable(args.Request);

        if (request.State != RMCERTRequestState.Arrived)
            return ResultFailure(request.Id, request.State, $"ERT request {request.Id} is not arrived.");

        request.State = RMCERTRequestState.Completed;
        request.LastError = string.Empty;
        request.LastWarning = string.Empty;
        request.RecruitmentEndsAt = null;
        UpdateSourceVisual(request, false);

        var adminText = GetAdminActorText(args.Actor, args.ActorName);
        AddERTRequestLog(LogImpact.Medium, "completed", request, adminText);
        DirtyState(request);
        return ResultSuccess(request);
    }

    private bool CheckRequirements(RMCERTRequest request, RMCERTCallPrototype call, out string error)
    {
        error = string.Empty;

        if (call.Requirements.DisallowDuringEvacuation &&
            _evacuation.IsEvacuationInProgress())
        {
            error = Loc.GetString("rmc-ert-error-unavailable-evacuation", ("call", call.Name));
            return false;
        }

        if (call.Requirements.DisallowDuringHijack &&
            _distressSignal.IsHijackActive())
        {
            error = Loc.GetString("rmc-ert-error-unavailable-hijack", ("call", call.Name));
            return false;
        }

        if (call.Requirements.MinRoundTime is { } minRound && _timing.CurTime < minRound)
        {
            error = Loc.GetString("rmc-ert-error-min-round-time",
                ("call", call.Name),
                ("minutes", (int)minRound.TotalMinutes));
            return false;
        }

        var dispatched = _requests.Values.Count(r =>
            r.Id != request.Id &&
            r.SelectedCall?.Id == call.ID &&
            r.State is RMCERTRequestState.Recruiting or RMCERTRequestState.Spawning or RMCERTRequestState.Launching or RMCERTRequestState.Arrived or RMCERTRequestState.Completed);

        if (call.Requirements.MaxCallsPerRound > 0 && dispatched >= call.Requirements.MaxCallsPerRound)
        {
            error = Loc.GetString("rmc-ert-error-max-calls-reached", ("call", call.Name));
            return false;
        }

        if (request.SourceEntity is { Valid: true } source &&
            _sourceCooldowns.TryGetValue(source, out var last) &&
            _timing.CurTime < last + call.Requirements.Cooldown &&
            request.CreatedAt != last)
        {
            error = Loc.GetString("rmc-ert-error-source-cooldown", ("call", call.Name));
            return false;
        }

        return true;
    }

    private bool CanCreateRequest(EntityUid source, out string reason)
    {
        reason = string.Empty;

        foreach (var request in _requests.Values)
        {
            if (request.SourceEntity != source)
                continue;

            if (!IsTerminal(request.State))
            {
                reason = Loc.GetString("rmc-ert-error-source-pending");
                return false;
            }
        }

        return true;
    }

    private bool TryGetPending(Guid id, out RMCERTRequest request)
    {
        if (_requests.TryGetValue(id, out request!) &&
            request.State == RMCERTRequestState.PendingAdmin)
        {
            return true;
        }

        return false;
    }

    private static RMCERTRequestResult ResultSuccess(RMCERTRequest request)
    {
        return new RMCERTRequestResult(true, request.Id, request.State, request.LastError, request.LastWarning);
    }

    private static RMCERTRequestResult ResultFailure(
        Guid requestId,
        RMCERTRequestState? state,
        string error,
        string warning = "")
    {
        return new RMCERTRequestResult(false, requestId, state, error, warning);
    }

    private RMCERTRequestResult RequestUnavailable(Guid id)
    {
        if (_requests.TryGetValue(id, out var request))
            return ResultFailure(id, request.State, $"ERT request {id} is not pending or available.", request.LastWarning);

        return ResultFailure(id, null, $"Unknown ERT request: {id}");
    }

    private static RMCERTRequestResult RequestTerminal(RMCERTRequest request)
    {
        return ResultFailure(request.Id, request.State, $"ERT request {request.Id} is already terminal.");
    }

    private void DirtyState()
    {
        var ev = new RMCERTStateChangedEvent();
        RaiseLocalEvent(ref ev);
    }

    private void DirtyState(RMCERTRequest request)
    {
        DirtyState();
    }

    private static bool MatchesAny(IReadOnlyCollection<string> left, IReadOnlyCollection<string> right)
    {
        if (left.Count == 0 || right.Count == 0)
            return false;

        return left.Any(right.Contains);
    }

    private static bool IsTerminal(RMCERTRequestState state)
    {
        return state is RMCERTRequestState.Denied or RMCERTRequestState.Cancelled or RMCERTRequestState.Failed or RMCERTRequestState.Completed;
    }
}
