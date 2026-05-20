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

    private void OnDropshipArrivedAtDestination(ref DropshipArrivedAtDestinationEvent ev)
    {
        foreach (var request in _requests.Values)
        {
            if (request.Shuttle != ev.Dropship.Owner)
                continue;

            if (request.State == RMCERTRequestState.Completed &&
                request.ShuttleHomeDestination is { } homeDestination &&
                ev.Destination == homeDestination)
            {
                if (request.ShuttleHomeIsFallback)
                {
                    CleanupRequestContent(request, Loc.GetString("rmc-ert-cleanup-reason-fallback-return"));
                    DirtyState(request);
                    return;
                }

                ReleasePrelaunchShuttleDoorLocks(ev.Dropship.Owner);
                _dropship.RaiseUpdate(ev.Dropship.Owner);
                return;
            }

            if (request.State != RMCERTRequestState.Launching)
            {
                continue;
            }

            if (request.SelectedCall is { } callId &&
                _prototypes.TryIndex(callId, out var call))
            {
                MarkArrived(request, call);
            }
            else
            {
                request.State = RMCERTRequestState.Arrived;
                request.LastError = string.Empty;
                request.LastWarning = string.Empty;
                MakeReturnRouteAvailable(request);
                UpdateSourceVisual(request, false);
                _chat.SendAdminAnnouncement(Loc.GetString("rmc-ert-admin-arrived-missing-call", ("id", request.Id)));
                AddERTRequestLog(LogImpact.Medium, "arrived with missing call prototype", request, "system");
                DirtyState(request);
            }

            return;
        }
    }

    private void OnDockingVerificationFailed(ref RMCDockingVerificationFailedEvent ev)
    {
        foreach (var request in _requests.Values)
        {
            if (request.State != RMCERTRequestState.Launching ||
                request.Shuttle != ev.Shuttle)
            {
                continue;
            }

            Log.Warning($"ERT request {request.Id} docking verification failed: {ev.Reason}. " +
                        $"eventRequest={ev.RequestId}, call={ev.Call ?? "none"}, class={ev.DockingClass}, " +
                        $"shuttle={FormatEntity(ev.Shuttle)}, destination={FormatEntity(ev.Destination)}, " +
                        $"targetGrid={FormatEntity(ev.TargetGrid)}, shuttleDock={FormatEntity(ev.ShuttleDock)}, " +
                        $"targetDock={FormatEntity(ev.TargetDock)}, actualShuttleDock={FormatEntity(ev.ActualShuttleDock)}, " +
                        $"actualTargetDock={FormatEntity(ev.ActualTargetDock)}");
            FailRequest(request, Loc.GetString("rmc-ert-error-docking-verification-failed"));
            return;
        }

        Log.Warning($"ERT docking verification failed for {FormatEntity(ev.Shuttle)}, but no launching ERT request matched it. " +
                    $"eventRequest={ev.RequestId}, reason={ev.Reason}");
    }

    private void OnERTShuttleFTLRequested(Entity<RMCERTShuttleComponent> ent, ref FTLRequestEvent args)
    {
        if (!TryGetReturnHomeFlight(ent.Owner, ent.Comp, out var request))
            return;

        var lockedDoors = ApplyShuttleDoorConsoleLocks(ent.Owner);
        Log.Info($"ERT request {request.Id} return launch locked {lockedDoors} shuttle doors for {ToPrettyString(ent.Owner)}.");
        _dropship.RaiseUpdate(ent.Owner);
    }

    private void OnERTShuttleFTLStarted(Entity<RMCERTShuttleComponent> ent, ref FTLStartedEvent args)
    {
        if (!TryGetReturnHomeFlight(ent.Owner, ent.Comp, out var request))
        {
            ReleasePrelaunchShuttleDoorLocks(ent.Owner);
            return;
        }

        ApplyShuttleDoorConsoleLocks(ent.Owner);
        SetShuttlePlayerRouteLock(ent.Owner, null);
        CompleteRequest(new RMCERTRequestActionArgs { Request = request.Id });
    }

    private bool TryAutoLaunch(Guid id)
    {
        if (!_requests.TryGetValue(id, out var request) ||
            request.State != RMCERTRequestState.Recruiting)
        {
            return false;
        }

        // Wait for both the ERT recruitment window and the underlying ghost-role raffles to finish before auto-launching.
        if (request.RecruitmentEndsAt is { } endsAt &&
            _timing.CurTime < endsAt)
        {
            request.NextAutoLaunchAttempt = endsAt;
            return false;
        }

        if (HasActiveRecruitmentRaffles(request))
        {
            request.NextAutoLaunchAttempt = _timing.CurTime + TimeSpan.FromSeconds(1);
            return false;
        }

        request.NextAutoLaunchAttempt = _timing.CurTime + TimeSpan.FromSeconds(1);
        return TryLaunch(id, null, null, true);
    }

    private bool TryLaunch(Guid id, EntityUid? admin, string? adminName, bool automatic)
    {
        if (!_requests.TryGetValue(id, out var request))
            return false;

        if (request.State != RMCERTRequestState.Recruiting)
            return false;

        if (request.SelectedCall is not { } callId || !_prototypes.TryIndex(callId, out var call))
        {
            FailRequest(request, Loc.GetString("rmc-ert-error-selected-call-missing"));
            return false;
        }

        if (!automatic && request.RecruitmentEndsAt is { } endsAt && _timing.CurTime < endsAt)
        {
            request.RecruitmentEndsAt = _timing.CurTime;
        }

        if (HasActiveRecruitmentRaffles(request))
        {
            request.LastError = Loc.GetString("rmc-ert-error-raffles-in-progress");
            request.LastWarning = string.Empty;
            if (!automatic)
            {
                AddERTRequestLog(LogImpact.Low,
                    "launch blocked",
                    request,
                    GetAdminActorText(admin, adminName),
                    call,
                    $"error=\"{FormatLogValue(request.LastError)}\"");
            }
            DirtyState(request);
            return false;
        }

        var launcher = automatic
            ? Loc.GetString("rmc-ert-launcher-automatic")
            : GetAdminActorText(admin, adminName);

        return TryLaunchRequest(request, call, launcher);
    }

    private bool TryLaunchRequest(RMCERTRequest request, RMCERTCallPrototype call, string launcher)
    {
        request.LastError = string.Empty;
        request.LastWarning = string.Empty;
        request.RecruitmentEndsAt = null;

        var acceptedCount = FinalizeRecruitment(request);
        Log.Info($"ERT request {request.Id} launching {call.ID}: accepted={acceptedCount}, " +
                 $"required={call.Requirements.MinRequiredSlots}, planned={request.PlannedRoster.Count}, " +
                 $"spawnedGhostRoles={request.SpawnedGhostRoles.Count}, launcher={launcher}, " +
                 $"Shuttle={FormatEntity(request.Shuttle)}, {GetShuttleDiagnostics(request, request.Shuttle)}");

        if (acceptedCount < call.Requirements.MinRequiredSlots)
        {
            FailRequest(request,
                acceptedCount == 0
                    ? Loc.GetString("rmc-ert-error-no-volunteers")
                    : Loc.GetString("rmc-ert-error-not-enough-volunteers",
                        ("accepted", acceptedCount),
                        ("required", call.Requirements.MinRequiredSlots)),
                announceNoResponse: true);
            return false;
        }

        if (request.Shuttle is not { Valid: true } shuttle)
        {
            request.State = RMCERTRequestState.Launching;
            AddERTRequestLog(LogImpact.High,
                "launched without shuttle",
                request,
                launcher,
                call,
                $"accepted={acceptedCount}, required={call.Requirements.MinRequiredSlots}");
            AnnounceDispatch(request, call);
            MarkArrived(request, call, Loc.GetString("rmc-ert-arrived-detail-no-shuttle", ("launcher", launcher)));
            return true;
        }

        if (!TryFindNavigationComputer(shuttle, out var computer))
        {
            FailRequest(request, Loc.GetString("rmc-ert-error-no-navigation-computer"));
            return false;
        }

        if (!TryFindLandingZone(request, call, computer, out var destination, out var landingZoneError))
        {
            LogLandingZoneDiagnostics(request, computer);
            FailRequest(request, !string.IsNullOrWhiteSpace(landingZoneError)
                ? landingZoneError
                : Loc.GetString("rmc-ert-error-no-landing-zone"));
            return false;
        }

        var shuttleTravelTime = call.ShuttleTravelTime is { } travelTime
            ? (float) travelTime.TotalSeconds
            : (float?) null;

        if (!_dropship.FlyTo(computer, destination, null, startupTime: 10f, hyperspaceTime: shuttleTravelTime))
        {
            FailRequest(request, Loc.GetString("rmc-ert-error-launch-failed"));
            return false;
        }

        request.State = RMCERTRequestState.Launching;
        AnnounceDispatch(request, call);
        _chat.SendAdminAnnouncement(Loc.GetString("rmc-ert-admin-launched",
            ("id", request.Id),
            ("call", call.Name),
            ("launcher", launcher)));
        AddERTRequestLog(LogImpact.High,
            "launched",
            request,
            launcher,
            call,
            $"destination={FormatEntity(destination)}, accepted={acceptedCount}, required={call.Requirements.MinRequiredSlots}");
        DirtyState(request);
        return true;
    }

    private bool TryFindNavigationComputer(EntityUid shuttle, out Entity<DropshipNavigationComputerComponent> computer)
    {
        var query = EntityQueryEnumerator<DropshipNavigationComputerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (xform.GridUid != shuttle)
                continue;

            computer = (uid, comp);
            return true;
        }

        computer = default;
        return false;
    }

    private bool TryFindLandingZone(
        RMCERTRequest request,
        RMCERTCallPrototype call,
        Entity<DropshipNavigationComputerComponent> computer,
        out EntityUid destination,
        out string error)
    {
        error = string.Empty;
        var candidates = new List<EntityUid>();
        var sourceMap = GetRequestSourceMap(request);

        // Keep arrivals on the same map as the originating request, then let the shared dropship rules filter by destination compatibility.
        var query = EntityQueryEnumerator<DropshipDestinationComponent>();
        while (query.MoveNext(out var uid, out var dropshipDestination))
        {
            if (dropshipDestination.Ship != null)
                continue;

            if (sourceMap != null &&
                Transform(uid).MapUid != sourceMap)
            {
                continue;
            }

            if (!_dropship.CanUseDestinationForShuttle(computer, uid, out _))
                continue;

            candidates.Add(uid);
        }

        if (candidates.Count > 0)
        {
            destination = _random.Pick(candidates);
            return true;
        }

        destination = default;
        return false;
    }

    private EntityUid? GetRequestSourceMap(RMCERTRequest request)
    {
        if (request.SourceEntity is not { Valid: true } source ||
            !TryComp(source, out TransformComponent? sourceXform))
        {
            return null;
        }

        return sourceXform.MapUid;
    }

    private bool TryPickRandomCall(RMCERTRequest request, out ProtoId<RMCERTCallPrototype>? callId, out string error)
    {
        callId = null;
        error = string.Empty;

        var calls = new List<RMCERTCallPrototype>();
        var total = 0;
        foreach (var id in request.AllowedCalls)
        {
            if (!_prototypes.TryIndex(id, out var call, false))
                continue;

            if (!call.Enabled || call.RandomWeight <= 0)
                continue;

            total += call.RandomWeight;
            calls.Add(call);
        }

        if (total <= 0)
        {
            error = Loc.GetString("rmc-ert-error-no-random-calls");
            return false;
        }

        var roll = _random.Next(total);
        var cursor = 0;
        foreach (var call in calls)
        {
            cursor += call.RandomWeight;
            if (roll >= cursor)
                continue;

            callId = new ProtoId<RMCERTCallPrototype>(call.ID);
            return true;
        }

        error = Loc.GetString("rmc-ert-error-random-selection-failed");
        return false;
    }

    private void MarkArrived(RMCERTRequest request, RMCERTCallPrototype call, string? detail = null)
    {
        request.State = RMCERTRequestState.Arrived;
        request.LastError = string.Empty;
        request.LastWarning = string.Empty;
        MakeReturnRouteAvailable(request);
        UpdateSourceVisual(request, false);
        Announce(call.Announcements.Arrival, request, call);

        var text = Loc.GetString("rmc-ert-admin-arrived",
            ("id", request.Id),
            ("call", call.Name));
        if (!string.IsNullOrWhiteSpace(detail))
        {
            text = Loc.GetString("rmc-ert-admin-arrived-detail",
                ("id", request.Id),
                ("call", call.Name),
                ("detail", detail));
        }

        _chat.SendAdminAnnouncement(text);
        AddERTRequestLog(LogImpact.Medium,
            "arrived",
            request,
            "system",
            call,
            string.IsNullOrWhiteSpace(detail) ? null : $"detail=\"{FormatLogValue(detail)}\"");
        DirtyState(request);
    }
}
