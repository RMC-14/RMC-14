using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction;
using Content.Shared.Shuttles.Components;
using Content.Shared.UserInterface;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Dropship;

public sealed partial class DropshipSystem
{
    private static readonly EntProtoId<IFFFactionComponent> AutopilotAnnouncementFaction = "FactionMarine";

    private TimeSpan _autopilotDefaultDelay;
    private TimeSpan _autopilotMinDelay;
    private TimeSpan _autopilotMaxDelay;
    private TimeSpan _autopilotRetryDelay;
    private TimeSpan _nextAutopilotUiRefresh;
    private bool _autopilotHijackStarted;

    private void InitializeAutopilot()
    {
        SubscribeLocalEvent<DropshipAutopilotComponent, ComponentInit>(OnAutopilotInit);
        SubscribeLocalEvent<DropshipRemoteControlConsoleComponent, ActivateInWorldEvent>(OnRemoteControlActivateInWorld, before: [typeof(ActivatableUISystem), typeof(ActivatableUIRequiresAccessSystem)]);
        SubscribeLocalEvent<DropshipRemoteControlConsoleComponent, ActivatableUIOpenAttemptEvent>(OnRemoteControlOpenAttempt);
        SubscribeLocalEvent<DropshipRemoteControlConsoleComponent, AfterActivatableUIOpenEvent>(OnRemoteControlConsoleOpen);
        Subs.CVar(_config, RMCCVars.RMCDropshipAutopilotDefaultDelaySeconds, v => _autopilotDefaultDelay = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_config, RMCCVars.RMCDropshipAutopilotMinDelaySeconds, v => _autopilotMinDelay = TimeSpan.FromSeconds(Math.Max(1, v)), true);
        Subs.CVar(_config, RMCCVars.RMCDropshipAutopilotMaxDelaySeconds, v => _autopilotMaxDelay = TimeSpan.FromSeconds(Math.Max(1, v)), true);
        Subs.CVar(_config, RMCCVars.RMCDropshipAutopilotRetrySeconds, v => _autopilotRetryDelay = TimeSpan.FromSeconds(Math.Max(1, v)), true);

        Subs.BuiEvents<DropshipRemoteControlConsoleComponent>(DropshipRemoteControlUiKey.Key,
            subs =>
            {
                subs.Event<DropshipRemoteLaunchMsg>(OnDropshipRemoteLaunchMsg);
                subs.Event<DropshipRemoteAutopilotConfigureMsg>(OnDropshipRemoteAutopilotConfigureMsg);
                subs.Event<DropshipRemoteAutopilotDisableMsg>(OnDropshipRemoteAutopilotDisableMsg);
                subs.Event<DropshipRemoteAutopilotLaunchNowMsg>(OnDropshipRemoteAutopilotLaunchNowMsg);
                subs.Event<DropshipRemoteAutopilotRecallNowMsg>(OnDropshipRemoteAutopilotRecallNowMsg);
            });

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnAutopilotRoundRestart);
        SubscribeLocalEvent<DropshipHijackStartEvent>(OnAutopilotHijack);
        SubscribeLocalEvent<DropshipHijackLandedEvent>(OnAutopilotHijack);
    }

    private void OnAutopilotInit(Entity<DropshipAutopilotComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Delay = ClampAutopilotDelay(ent.Comp.Delay == TimeSpan.Zero ? _autopilotDefaultDelay : ent.Comp.Delay);
        Dirty(ent);
    }

    private void OnAutopilotRoundRestart(RoundRestartCleanupEvent ev)
    {
        _nextAutopilotUiRefresh = default;
        _autopilotHijackStarted = false;
    }

    private void OnAutopilotHijack<T>(ref T args)
    {
        if (_autopilotHijackStarted)
            return;

        _autopilotHijackStarted = true;
        var reason = Loc.GetString("rmc-dropship-autopilot-status-detail-hijack-disabled");
        var query = EntityQueryEnumerator<DropshipComponent, DropshipAutopilotComponent>();
        while (query.MoveNext(out var uid, out var dropship, out var autopilot))
        {
            if (!TerminatingOrDeleted(uid) && autopilot.Mode != DropshipAutopilotMode.Disabled)
                SetAutopilotDisabled((uid, dropship), autopilot, null, reason);
        }

        RefreshUI();
        RefreshRemoteControlConsoles();
    }

    private TimeSpan ClampAutopilotDelay(TimeSpan delay)
    {
        var min = Math.Min(_autopilotMinDelay.TotalSeconds, _autopilotMaxDelay.TotalSeconds);
        var max = Math.Max(_autopilotMinDelay.TotalSeconds, _autopilotMaxDelay.TotalSeconds);
        var seconds = Math.Clamp(delay.TotalSeconds, min, max);
        return TimeSpan.FromSeconds(seconds);
    }

    private void SetAutopilotStatus(
        EntityUid dropship,
        DropshipAutopilotComponent autopilot,
        DropshipAutopilotStatus status,
        string details)
    {
        if (autopilot.Status == status && autopilot.StatusDetails == details)
            return;

        autopilot.Status = status;
        autopilot.StatusDetails = details;
        Dirty(dropship, autopilot);
    }

    private void SetAutopilotDisabled(
        Entity<DropshipComponent> dropship,
        DropshipAutopilotComponent autopilot,
        EntityUid? actor,
        string reason)
    {
        var wasActive = autopilot.Mode != DropshipAutopilotMode.Disabled;
        autopilot.Mode = DropshipAutopilotMode.Disabled;
        autopilot.NextDepartureAt = null;
        autopilot.RetryAt = null;
        autopilot.Status = DropshipAutopilotStatus.Offline;
        autopilot.StatusDetails = reason;
        Dirty(dropship.Owner, autopilot);

        if (!wasActive)
            return;

        _adminLog.Add(LogType.RMCDropshipAutopilot,
            $"{ToPrettyString(actor):player} disabled autopilot on {ToPrettyString(dropship):dropship}: {reason}");
    }

    private void DisableAutopilotForManualOverride(
        Entity<DropshipComponent> dropship,
        DropshipAutopilotComponent autopilot,
        DropshipLaunchSource source,
        EntityUid? actor)
    {
        if (source is not (DropshipLaunchSource.ManualNavigation or DropshipLaunchSource.RemoteNavigation or DropshipLaunchSource.PlanetsideTerminal or DropshipLaunchSource.Hijack))
            return;

        SetAutopilotDisabled(dropship, autopilot, actor,
            GetAutopilotOverrideReason(source));
    }

    private string GetAutopilotOverrideReason(DropshipLaunchSource source)
    {
        return source switch
        {
            DropshipLaunchSource.ManualNavigation => Loc.GetString("rmc-dropship-autopilot-status-detail-override-manual"),
            DropshipLaunchSource.RemoteNavigation => Loc.GetString("rmc-dropship-autopilot-status-detail-override-remote"),
            DropshipLaunchSource.PlanetsideTerminal => Loc.GetString("rmc-dropship-autopilot-status-detail-override-planetside"),
            DropshipLaunchSource.Hijack => Loc.GetString("rmc-dropship-autopilot-status-detail-override-hijack"),
            _ => Loc.GetString("rmc-dropship-autopilot-status-detail-disabled"),
        };
    }

    private bool TryGetNextAutopilotTarget(
        Entity<DropshipComponent> dropship,
        DropshipAutopilotComponent autopilot,
        out EntityUid target,
        out string reason)
    {
        target = default;
        reason = string.Empty;

        if (autopilot.Mode == DropshipAutopilotMode.Disabled)
        {
            reason = Loc.GetString("rmc-dropship-autopilot-status-detail-disabled");
            return false;
        }

        if (!TryResolveRouteHangar(dropship, autopilot, out var routeHangar))
        {
            reason = Loc.GetString("rmc-dropship-autopilot-status-detail-select-hangar");
            return false;
        }

        if (autopilot.Mode == DropshipAutopilotMode.RecallOnly)
        {
            if (dropship.Comp.Destination == routeHangar)
            {
                reason = Loc.GetString("rmc-dropship-autopilot-status-detail-holding-hangar");
                return false;
            }

            target = routeHangar;
            return true;
        }

        if (!TryResolveLandingZone(autopilot, out var landingZone))
        {
            reason = Loc.GetString("rmc-dropship-autopilot-status-detail-lz-invalid");
            return false;
        }

        target = dropship.Comp.Destination == routeHangar ? landingZone : routeHangar;
        return true;
    }

    private void LaunchAutopilotNow(
        Entity<DropshipComponent> dropship,
        DropshipAutopilotComponent autopilot,
        Entity<DropshipNavigationComputerComponent> computer,
        EntityUid target)
    {
        autopilot.NextDepartureAt = null;
        autopilot.RetryAt = null;

        if (!CanAutopilotLaunchTo(computer, dropship, target, out var reason))
        {
            autopilot.RetryAt = _timing.CurTime + _autopilotRetryDelay;
            Dirty(dropship.Owner, autopilot);
            SetAutopilotStatus(dropship, autopilot, DropshipAutopilotStatus.Blocked, reason);
            return;
        }

        var departure = dropship.Comp.Destination;
        if (FlyTo(computer, target, null, source: DropshipLaunchSource.Autopilot))
        {
            Dirty(dropship.Owner, autopilot);
            SetAutopilotStatus(dropship, autopilot, DropshipAutopilotStatus.InFlight,
                Loc.GetString("rmc-dropship-autopilot-status-detail-launching", ("destination", Name(target))));
            if (departure is { } location && Exists(location))
            {
                AnnounceAutopilot(computer, dropship,
                    Loc.GetString("rmc-dropship-autopilot-departing",
                        ("ship", Name(dropship)), ("location", Name(location))));
            }
            return;
        }

        autopilot.RetryAt = _timing.CurTime + _autopilotRetryDelay;
        Dirty(dropship.Owner, autopilot);
        SetAutopilotStatus(dropship, autopilot, DropshipAutopilotStatus.Blocked,
            Loc.GetString("rmc-dropship-autopilot-status-detail-busy"));
    }

    private void UpdateAutopilot(EntityUid uid, DropshipComponent dropship, DropshipAutopilotComponent autopilot, TimeSpan time)
    {
        if (autopilot.Mode == DropshipAutopilotMode.Disabled)
            return;

        if (_autopilotHijackStarted)
        {
            SetAutopilotDisabled((uid, dropship), autopilot, null,
                Loc.GetString("rmc-dropship-autopilot-status-detail-hijack-disabled"));
            return;
        }

        if (dropship.Crashed)
        {
            SetAutopilotDisabled((uid, dropship), autopilot, null, Loc.GetString("rmc-dropship-autopilot-status-detail-crashed-disabled"));
            return;
        }

        if (HasComp<FTLComponent>(uid))
        {
            SetAutopilotStatus(uid, autopilot, DropshipAutopilotStatus.InFlight,
                Loc.GetString("rmc-dropship-autopilot-status-detail-waiting-ftl"));
            return;
        }

        if (IsDropshipPreFlightFueling(out var remaining))
        {
            autopilot.NextDepartureAt = null;
            autopilot.RetryAt = time + _autopilotRetryDelay;
            Dirty(uid, autopilot);
            SetAutopilotStatus(uid, autopilot, DropshipAutopilotStatus.Waiting,
                Loc.GetString("rmc-dropship-autopilot-status-detail-preflight",
                    ("minutes", Math.Max(1, (int)remaining.TotalMinutes))));
            return;
        }

        if (autopilot.RetryAt is { } retryAt && retryAt > time)
            return;

        if (!TryGetNextAutopilotTarget((uid, dropship), autopilot, out var target, out var reason))
        {
            autopilot.NextDepartureAt = null;
            autopilot.RetryAt = time + _autopilotRetryDelay;
            Dirty(uid, autopilot);
            var holdingAtRouteHangar = Loc.GetString("rmc-dropship-autopilot-status-detail-holding-hangar");
            SetAutopilotStatus(uid, autopilot,
                reason == holdingAtRouteHangar ? DropshipAutopilotStatus.Ready : DropshipAutopilotStatus.Error,
                reason);
            return;
        }

        if (!TryGetDropshipComputer(uid, out var computer))
        {
            autopilot.NextDepartureAt = null;
            autopilot.RetryAt = time + _autopilotRetryDelay;
            Dirty(uid, autopilot);
            SetAutopilotStatus(uid, autopilot, DropshipAutopilotStatus.Error,
                Loc.GetString("rmc-dropship-autopilot-status-detail-no-computer"));
            return;
        }

        if (!CanAutopilotLaunchTo(computer, (uid, dropship), target, out reason))
        {
            autopilot.NextDepartureAt = null;
            autopilot.RetryAt = time + _autopilotRetryDelay;
            Dirty(uid, autopilot);
            SetAutopilotStatus(uid, autopilot, DropshipAutopilotStatus.Blocked, reason);
            return;
        }

        if (autopilot.NextDepartureAt == null)
        {
            var delay = ClampAutopilotDelay(autopilot.Delay);
            autopilot.NextDepartureAt = time + delay;
            autopilot.RetryAt = null;
            Dirty(uid, autopilot);
            SetAutopilotStatus(uid, autopilot, DropshipAutopilotStatus.Waiting,
                Loc.GetString("rmc-dropship-autopilot-status-detail-waiting-launch", ("destination", Name(target))));
            if (dropship.Destination is { } location && Exists(location))
            {
                AnnounceAutopilot(computer, uid,
                    Loc.GetString("rmc-dropship-autopilot-departure-warning",
                        ("ship", Name(uid)), ("location", Name(location)),
                        ("seconds", (int)delay.TotalSeconds)));
            }
            return;
        }

        if (autopilot.NextDepartureAt > time)
            return;

        LaunchAutopilotNow((uid, dropship), autopilot, computer, target);
    }

    private void AnnounceAutopilot(Entity<DropshipNavigationComputerComponent> computer, EntityUid dropship, string message)
    {
        if (computer.Comp.Faction == AutopilotAnnouncementFaction)
            _marineAnnounce.AnnounceARESStaging(dropship, message);
    }

    private void UpdateAutopilots(TimeSpan time)
    {
        if (time < _nextAutopilotUiRefresh)
            return;

        _nextAutopilotUiRefresh = time + TimeSpan.FromSeconds(1);
        var query = EntityQueryEnumerator<DropshipComponent, DropshipAutopilotComponent>();
        while (query.MoveNext(out var uid, out var dropship, out var autopilot))
        {
            if (!TerminatingOrDeleted(uid))
                UpdateAutopilot(uid, dropship, autopilot, time);
        }

        RefreshUI();
        RefreshRemoteControlConsoles();
    }
}
