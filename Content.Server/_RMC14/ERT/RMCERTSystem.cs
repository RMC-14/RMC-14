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
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.ERT;

/// <summary>
/// Orchestrates the full ERT flow: request creation, admin approval, roster planning, ghost-role recruitment, shuttle launch and cleanup.
/// </summary>
public sealed class RMCERTSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly CMDistressSignalRuleSystem _distressSignal = default!;
    [Dependency] private readonly DropshipSystem _dropship = default!;
    [Dependency] private readonly RMCERTDiscordWebhookSystem _discordWebhook = default!;
    [Dependency] private readonly SharedEvacuationSystem _evacuation = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RandomHumanoidSystem _randomHumanoid = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private readonly Dictionary<Guid, RMCERTRequest> _requests = new();
    private readonly Dictionary<EntityUid, TimeSpan> _sourceCooldowns = new();
    private readonly Dictionary<EntityUid, EntityUid> _pendingHandheldDialogs = new();
    private readonly HashSet<ICommonSession> _queuedPendingAdminNotifications = [];

    private MapId? _ertMap;
    private int _loadedShuttles;

    private static readonly SoundPathSpecifier DistressBeaconSound = new("/Audio/_RMC14/AI/distressbeacon.ogg");
    private static readonly SoundPathSpecifier DistressReceivedSound = new("/Audio/_RMC14/AI/distressreceived.ogg");
    private static readonly SoundPathSpecifier AdminRequestSound = new("/Audio/_RMC14/Effects/sos-morse-code.ogg");
    private static readonly EntProtoId ERTShuttleReturnDestinationPrototype = "RMCERTShuttleReturnDestination";
    private static readonly EntProtoId ERTShuttleReturnDestinationSmallPrototype = "RMCERTShuttleReturnDestinationSmall";
    private static readonly EntProtoId ERTShuttleReturnDestinationBigPrototype = "RMCERTShuttleReturnDestinationBig";
    private static readonly TimeSpan ReturnDelay = TimeSpan.FromMinutes(2);
    // AdminManager raises OnPermsChanged before the client enables admin chat filters.
    private static readonly TimeSpan PendingAdminNotificationDelay = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<DropshipArrivedAtDestinationEvent>(OnDropshipArrivedAtDestination);
        SubscribeLocalEvent<RMCDockingVerificationFailedEvent>(OnDockingVerificationFailed);

        SubscribeLocalEvent<RMCERTDistressBeaconComponent, UseInHandEvent>(OnHandheldUse);
        SubscribeLocalEvent<ActorComponent, RMCERTHandheldDistressReasonEvent>(OnHandheldReason);
        SubscribeLocalEvent<RMCERTMemberComponent, MindAddedMessage>(OnERTMindAdded);
        SubscribeLocalEvent<RMCERTShuttleComponent, FTLRequestEvent>(OnERTShuttleFTLRequested);
        SubscribeLocalEvent<RMCERTShuttleComponent, FTLStartedEvent>(OnERTShuttleFTLStarted);
        SubscribeLocalEvent<MarineCommunicationsComputerComponent, RMCERTConsoleDistressReasonEvent>(OnConsoleReason);
        Subs.BuiEvents<MarineCommunicationsComputerComponent>(MarineCommunicationsComputerUI.Key, subs =>
        {
            subs.Event<MarineCommunicationsDistressBeaconMsg>(OnMarineCommunicationsDistressBeacon);
        });

        _adminManager.OnPermsChanged += OnAdminPermsChanged;
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        ValidatePrototypes();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _adminManager.OnPermsChanged -= OnAdminPermsChanged;
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var request in _requests.Values.ToArray())
        {
            if (request.State == RMCERTRequestState.Arrived)
            {
                TryUnlockReturnRoute(request);
                continue;
            }

            if (request.State != RMCERTRequestState.Recruiting)
                continue;

            if (request.SelectedCall is not { } callId || !_prototypes.TryIndex(callId, out var call))
                continue;

            if (!call.AutoLaunch)
                continue;

            if (_timing.CurTime < request.NextAutoLaunchAttempt)
                continue;

            TryAutoLaunch(request.Id);
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _requests.Clear();
        _sourceCooldowns.Clear();
        _pendingHandheldDialogs.Clear();
        _queuedPendingAdminNotifications.Clear();
        _discordWebhook.ClearRequests();
        _ertMap = null;
        _loadedShuttles = 0;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<RMCERTCallPrototype>() || ev.WasModified<EntityPrototype>())
            ValidatePrototypes();
    }

    public RMCERTAdminEuiState CreateAdminState(bool canForceCalls)
    {
        // The admin window works off a flattened snapshot so the client does not need to know about runtime-only request objects.
        var requests = _requests.Values
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RMCERTRequestOption(
                r.Id,
                r.State,
                r.Source,
                r.SourceName,
                r.RequesterName,
                r.Reason,
                GetSelectedCallLabel(r),
                r.AllowedCalls.Select(c => c.Id).ToList(),
                FormatRoundTime(r.CreatedAt),
                r.LastError,
                r.LastWarning))
            .ToList();

        var calls = _prototypes.EnumeratePrototypes<RMCERTCallPrototype>()
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Name)
            .Select(ToCallOption)
            .ToList();

        var forceCalls = canForceCalls
            ? GetForceCallOptions()
            : [];

        return new RMCERTAdminEuiState(requests, calls, canForceCalls, forceCalls);
    }

    public List<RMCERTCallOption> GetForceCallOptions()
    {
        return _prototypes.EnumeratePrototypes<RMCERTCallPrototype>()
            .Where(IsForceCallable)
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Name)
            .Select(ToCallOption)
            .ToList();
    }

    public bool IsForceCallable(RMCERTCallPrototype call)
    {
        return call.Enabled && call.AdminSelectable;
    }

    private RMCERTCallOption ToCallOption(RMCERTCallPrototype call)
    {
        return new RMCERTCallOption(
            call.ID,
            call.Name,
            GetOrganizationLabel(call),
            call.Category,
            call.RandomWeight,
            call.AdminSelectable,
            call.AdminButtonLabel);
    }

    public void RequestConsoleDistress(EntityUid console, EntityUid user, string reason)
    {
        var calls = _prototypes.EnumeratePrototypes<RMCERTCallPrototype>()
            .Where(c => c.Enabled && c.AllowedSources.Contains(RMCERTRequestSource.Console))
            .Select(c => new ProtoId<RMCERTCallPrototype>(c.ID))
            .ToList();

        CreateRequest(RMCERTRequestSource.Console, console, user, reason, calls);
        _ui.CloseUi(console, MarineCommunicationsComputerUI.Key, user);
    }

    public void RequestHandheldDistress(Entity<RMCERTDistressBeaconComponent> beacon, EntityUid user, string reason)
    {
        reason = reason.Trim();

        if (beacon.Comp.Spent)
        {
            _popup.PopupEntity(Loc.GetString("rmc-ert-popup-beacon-spent"), beacon, user, PopupType.MediumCaution);
            return;
        }

        if (_timing.CurTime < beacon.Comp.LastUsed + beacon.Comp.Cooldown)
        {
            _popup.PopupEntity(Loc.GetString("rmc-ert-popup-beacon-cooldown"), beacon, user, PopupType.MediumCaution);
            return;
        }

        if (beacon.Comp.ReasonRequired && string.IsNullOrWhiteSpace(reason))
        {
            _popup.PopupEntity(Loc.GetString("rmc-ert-popup-beacon-reason-required"), beacon, user, PopupType.MediumCaution);
            return;
        }

        if (!TryGetHandheldAvailableCalls(beacon, user, reason, out var calls, out var error))
        {
            _popup.PopupEntity(error, beacon, user, PopupType.MediumCaution);
            return;
        }

        beacon.Comp.LastUsed = _timing.CurTime;

        CreateRequest(RMCERTRequestSource.Handheld, beacon, user, reason, calls);
    }

    public bool ForceCall(
        ProtoId<RMCERTCallPrototype> callId,
        EntityUid? admin,
        string? adminName,
        string reason,
        out Guid requestId,
        out string error)
    {
        requestId = Guid.Empty;
        error = string.Empty;
        reason = reason.Trim();

        if (!_prototypes.TryIndex(callId, out var call))
        {
            error = Loc.GetString("rmc-ert-error-unknown-call", ("id", callId.Id));
            _adminLog.Add(LogType.RMCAdminCommandLogging,
                LogImpact.Medium,
                $"ERT force call failed: actor={FormatLogValue(GetAdminActorText(admin, adminName))}, call={FormatLogValue(callId.Id)}, reason=\"{FormatLogValue(reason)}\", error=\"{FormatLogValue(error)}\"");
            return false;
        }

        if (!IsForceCallable(call))
        {
            error = Loc.GetString("rmc-ert-error-call-not-force-callable", ("call", call.Name));
            _adminLog.Add(LogType.RMCAdminCommandLogging,
                LogImpact.Medium,
                $"ERT force call failed: actor={FormatLogValue(GetAdminActorText(admin, adminName))}, call={FormatLogValue(call.Name)}, prototype={call.ID}, reason=\"{FormatLogValue(reason)}\", error=\"{FormatLogValue(error)}\"");
            return false;
        }

        var request = new RMCERTRequest
        {
            Source = RMCERTRequestSource.Admin,
            Requester = admin,
            SourceName = Loc.GetString("rmc-ert-source-admin"),
            RequesterName = GetAdminActorText(admin, adminName),
            Reason = reason,
            CreatedAt = _timing.CurTime,
            AllowedCalls = [callId],
        };

        _requests[request.Id] = request;
        requestId = request.Id;
        AddERTRequestLog(LogImpact.High, "force requested", request, request.RequesterName, call);

        if (ApproveSpecific(request.Id, callId, admin, false, request.RequesterName, true))
            return true;

        error = !string.IsNullOrWhiteSpace(request.LastError)
            ? request.LastError
            : !string.IsNullOrWhiteSpace(request.LastWarning)
                ? request.LastWarning
                : Loc.GetString("rmc-ert-error-force-call-failed", ("call", call.Name));
        return false;
    }

    public bool ApproveRandom(Guid id, EntityUid? admin)
    {
        if (!TryGetPending(id, out var request))
            return false;

        if (!TryPickRandomCall(request, out var call, out var error))
        {
            FailRequest(request, error);
            return false;
        }

        if (call is not { } selected)
            return false;

        return ApproveSpecific(id, selected, admin, true);
    }

    public bool ApproveSpecific(Guid id, ProtoId<RMCERTCallPrototype> callId, EntityUid? admin)
    {
        return ApproveSpecific(id, callId, admin, false);
    }

    private bool ApproveSpecific(
        Guid id,
        ProtoId<RMCERTCallPrototype> callId,
        EntityUid? admin,
        bool randomSelection,
        string? adminOverrideName = null,
        bool forced = false)
    {
        if (!TryGetPending(id, out var request))
            return false;

        if (!randomSelection && request.Source == RMCERTRequestSource.Console)
        {
            request.LastError = Loc.GetString("rmc-ert-error-console-random-only");
            request.LastWarning = string.Empty;
            DirtyState(request);
            return false;
        }

        if (!_prototypes.TryIndex(callId, out var call))
        {
            FailRequest(request, Loc.GetString("rmc-ert-error-unknown-call", ("id", callId.Id)));
            return false;
        }

        if (!request.AllowedCalls.Contains(callId) && request.Source != RMCERTRequestSource.Admin)
        {
            FailRequest(request, Loc.GetString("rmc-ert-error-call-not-allowed", ("call", call.Name)));
            return false;
        }

        if (!call.Enabled)
        {
            FailRequest(request, Loc.GetString("rmc-ert-error-call-disabled", ("call", call.Name)));
            return false;
        }

        if (!CheckRequirements(request, call, out var error))
        {
            FailRequest(request, error);
            return false;
        }

        if (!TryPrepareRequestForDispatch(request, call, true, out error))
        {
            request.LastError = string.Empty;
            request.LastWarning = error;
            AddERTRequestLog(LogImpact.Medium,
                forced ? "force approve blocked" : "approve blocked",
                request,
                GetAdminActorText(admin, adminOverrideName),
                call,
                $"warning=\"{FormatLogValue(error)}\"");
            DirtyState(request);
            return false;
        }

        request.State = RMCERTRequestState.PendingDispatch;
        request.SelectedCall = callId;
        request.DispatchAt = _timing.CurTime + TimeSpan.FromSeconds(call.LaunchDelay);
        request.LastError = string.Empty;
        request.LastWarning = string.Empty;

        var adminText = GetAdminActorText(admin, adminOverrideName);
        _chat.SendAdminAnnouncement(forced
            ? GetForceCallAnnouncement(request, call, adminText)
            : Loc.GetString("rmc-ert-admin-approved",
                ("admin", adminText),
                ("id", request.Id),
                ("call", call.Name),
                ("delay", (int)call.LaunchDelay)));
        Log.Info($"ERT request {request.Id} approved as {call.ID} by {adminText}");
        AddERTRequestLog(forced ? LogImpact.High : LogImpact.Medium,
            forced ? "force approved" : "approved",
            request,
            adminText,
            call,
            $"selection={(randomSelection ? "random" : "specific")}, dispatchDelay={call.LaunchDelay:0}s");

        AnnounceDistressLaunched(request, call);

        DirtyState(request);
        Timer.Spawn(TimeSpan.FromSeconds(call.LaunchDelay), () => Dispatch(request.Id));
        return true;
    }

    public bool Deny(Guid id, EntityUid? admin)
    {
        if (!_requests.TryGetValue(id, out var request))
            return false;

        if (IsTerminal(request.State))
            return false;

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

        var adminText = admin is { Valid: true }
            ? ToPrettyString(admin.Value)
            : Loc.GetString("rmc-ert-admin-actor-server");
        _chat.SendAdminAnnouncement(Loc.GetString("rmc-ert-admin-denied",
            ("admin", adminText),
            ("id", request.Id),
            ("requester", request.RequesterName)));
        AddERTRequestLog(LogImpact.Medium, "denied", request, adminText);

        if (request.SelectedCall is { } callId && _prototypes.TryIndex(callId, out var call))
            AnnounceNoResponse(request, call);
        else
            AnnounceNoResponse(request, null);

        DirtyState(request);
        return true;
    }

    public bool Cancel(Guid id, EntityUid? admin)
    {
        if (!_requests.TryGetValue(id, out var request))
            return false;

        if (IsTerminal(request.State))
            return false;

        request.State = RMCERTRequestState.Cancelled;
        request.LastError = string.Empty;
        request.LastWarning = string.Empty;
        request.RecruitmentEndsAt = null;
        CleanupRequestContent(request, Loc.GetString("rmc-ert-cleanup-reason-cancelled"));
        UpdateSourceVisual(request, false);
        var adminText = admin is { Valid: true }
            ? ToPrettyString(admin.Value)
            : Loc.GetString("rmc-ert-admin-actor-server");
        _chat.SendAdminAnnouncement(Loc.GetString("rmc-ert-admin-cancelled",
            ("admin", adminText),
            ("id", request.Id)));
        AddERTRequestLog(LogImpact.High, "cancelled", request, adminText);

        if (request.SelectedCall is { } callId && _prototypes.TryIndex(callId, out var call))
            Announce(call.Announcements.Cancelled, request, call);

        DirtyState(request);
        return true;
    }

    public bool Launch(Guid id, EntityUid? admin)
    {
        return TryLaunch(id, admin, false);
    }

    public bool Complete(Guid id, EntityUid? admin)
    {
        if (!_requests.TryGetValue(id, out var request))
            return false;

        if (request.State != RMCERTRequestState.Arrived)
            return false;

        request.State = RMCERTRequestState.Completed;
        request.LastError = string.Empty;
        request.LastWarning = string.Empty;
        request.RecruitmentEndsAt = null;
        request.ReturnAvailableAt = null;
        request.ReturnRouteUnlocked = false;
        UpdateSourceVisual(request, false);

        var adminText = admin is { Valid: true }
            ? ToPrettyString(admin.Value)
            : Loc.GetString("rmc-ert-admin-actor-server");
        var selected = GetSelectedCallLabel(request) ?? Loc.GetString("rmc-ert-response-team-fallback");
        _chat.SendAdminAnnouncement(Loc.GetString("rmc-ert-admin-completed",
            ("admin", adminText),
            ("id", request.Id),
            ("team", selected)));
        AddERTRequestLog(LogImpact.Medium, "completed", request, adminText);
        DirtyState(request);
        return true;
    }

    private void OnMarineCommunicationsDistressBeacon(Entity<MarineCommunicationsComputerComponent> ent, ref MarineCommunicationsDistressBeaconMsg args)
    {
        if (!ent.Comp.CanTransmitDistress)
        {
            _popup.PopupEntity(Loc.GetString("rmc-ert-popup-console-unavailable"), ent, args.Actor, PopupType.MediumCaution);
            return;
        }

        if (!CanCreateRequest(ent, out var reason))
        {
            _popup.PopupEntity(reason, ent, args.Actor, PopupType.MediumCaution);
            return;
        }

        var ev = new RMCERTConsoleDistressReasonEvent(GetNetEntity(args.Actor));
        _dialog.OpenInput(ent, args.Actor, Loc.GetString("rmc-ert-prompt-console-reason"), ev, true, ent.Comp.DistressReasonLimit);
    }

    private void OnConsoleReason(Entity<MarineCommunicationsComputerComponent> ent, ref RMCERTConsoleDistressReasonEvent args)
    {
        if (!TryGetEntity(args.User, out var user))
            return;

        RequestConsoleDistress(ent, user.Value, args.Message);
    }

    private void OnERTMindAdded(Entity<RMCERTMemberComponent> ent, ref MindAddedMessage args)
    {
        if (!TryComp(ent, out ActorComponent? actor) ||
            !_requests.TryGetValue(ent.Comp.RequestId, out var request) ||
            !_prototypes.TryIndex(new ProtoId<RMCERTCallPrototype>(ent.Comp.Call), out var call))
        {
            return;
        }

        var briefing = BuildMemberBriefing(request, call);
        if (string.IsNullOrWhiteSpace(briefing))
            return;

        _chat.DispatchServerMessage(actor.PlayerSession, briefing, false);
    }

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
                BeginReturnDelay(request);
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
        Complete(request.Id, null);
    }

    private void OnHandheldUse(Entity<RMCERTDistressBeaconComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp(ent, out AccessReaderComponent? access) &&
            !_access.IsAllowed(args.User, ent, access))
        {
            _popup.PopupEntity(Loc.GetString("rmc-access-denied"), ent, args.User, PopupType.MediumCaution);
            args.Handled = true;
            return;
        }

        if (!CanCreateRequest(ent, out var reason))
        {
            _popup.PopupEntity(reason, ent, args.User, PopupType.MediumCaution);
            args.Handled = true;
            return;
        }

        if (ent.Comp.ReasonRequired)
        {
            // Attach the dialog to the user, not the held item. Keep the beacon association server-side so the
            // networked dialog state does not need to serialize an inventory-entity reference back to the client.
            _pendingHandheldDialogs[args.User] = ent;
            var ev = new RMCERTHandheldDistressReasonEvent();
            _dialog.OpenInput(args.User, Loc.GetString("rmc-ert-prompt-handheld-reason", ("title", ent.Comp.RequestTitle)), ev, true, ent.Comp.ReasonLimit);
        }
        else
        {
            RequestHandheldDistress(ent, args.User, string.Empty);
        }

        args.Handled = true;
    }

    private void OnHandheldReason(Entity<ActorComponent> ent, ref RMCERTHandheldDistressReasonEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (!_pendingHandheldDialogs.Remove(ent.Owner, out var beaconUid) ||
            !TryComp(beaconUid, out RMCERTDistressBeaconComponent? beacon))
        {
            return;
        }

        RequestHandheldDistress((beaconUid, beacon), ent.Owner, args.Message);
    }

    private void CreateRequest(
        RMCERTRequestSource source,
        EntityUid sourceEntity,
        EntityUid requester,
        string reason,
        List<ProtoId<RMCERTCallPrototype>> allowedCalls)
    {
        if (!CanCreateRequest(sourceEntity, out var error))
        {
            _popup.PopupEntity(error, sourceEntity, requester, PopupType.MediumCaution);
            return;
        }

        if (allowedCalls.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-ert-popup-no-source-teams"), sourceEntity, requester, PopupType.MediumCaution);
            return;
        }

        reason = reason.Trim();
        var request = new RMCERTRequest
        {
            Source = source,
            SourceEntity = sourceEntity,
            Requester = requester,
            SourceName = Name(sourceEntity),
            RequesterName = Name(requester),
            Reason = reason,
            CreatedAt = _timing.CurTime,
            AllowedCalls = allowedCalls,
        };

        _requests[request.Id] = request;
        _sourceCooldowns[sourceEntity] = _timing.CurTime;
        AddERTRequestLog(LogImpact.Medium,
            "created",
            request,
            GetRequesterLogText(request),
            extra: $"allowedCalls=[{string.Join(", ", allowedCalls.Select(c => c.Id))}]");

        var text = BuildAdminRequestAnnouncement(request);
        _chat.SendAdminAnnouncement(text);

        NotifyAdminsOfRequest();

        if (source == RMCERTRequestSource.Handheld)
            UpdateSourceVisual(request, true);

        _popup.PopupEntity(GetRequestSuccessText(sourceEntity, source), sourceEntity, requester, PopupType.Medium);
        DirtyState(request);
    }

    private void OnAdminPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (!args.IsAdmin)
            return;

        QueuePendingAdminRequestNotification(args.Player, "admin permissions changed");
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.InGame)
            return;

        QueuePendingAdminRequestNotification(args.Session, "session entered game");
    }

    private void QueuePendingAdminRequestNotification(ICommonSession session, string reason)
    {
        if (!_queuedPendingAdminNotifications.Add(session))
        {
            Log.Debug($"Skipped duplicate pending ERT request notification queue for {session.Name}: {reason}.");
            return;
        }

        Log.Debug($"Queued pending ERT request notification for {session.Name}: {reason}.");
        Timer.Spawn(PendingAdminNotificationDelay, () =>
        {
            _queuedPendingAdminNotifications.Remove(session);

            if (session.Status == SessionStatus.Disconnected)
            {
                Log.Debug($"Skipped pending ERT request notification for {session.Name}: session disconnected.");
                return;
            }

            NotifyAdminOfPendingRequests(session);
        });
    }

    private void NotifyAdminOfPendingRequests(ICommonSession session)
    {
        if (!_adminManager.IsAdmin(session))
        {
            Log.Debug($"Skipped pending ERT request notification for {session.Name}: session is not an active admin.");
            return;
        }

        var sent = 0;
        foreach (var request in _requests.Values)
        {
            if (request.State != RMCERTRequestState.PendingAdmin)
                continue;

            _chat.SendAdminAnnouncementMessage(session, BuildAdminRequestAnnouncement(request));
            sent++;
        }

        Log.Debug($"Sent {sent} pending ERT request notifications to {session.Name}.");
    }

    private void Dispatch(Guid id)
    {
        if (!_requests.TryGetValue(id, out var request))
            return;

        if (request.State != RMCERTRequestState.PendingDispatch)
            return;

        if (request.SelectedCall is not { } callId || !_prototypes.TryIndex(callId, out var call))
        {
            FailRequest(request, Loc.GetString("rmc-ert-error-selected-call-missing"));
            return;
        }

        request.State = RMCERTRequestState.Spawning;
        DirtyState(request);

        // Build the shuttle and the final roster up front so ghost-role raffles reflect the team that will actually deploy.
        if (!TryPrepareRequestForDispatch(request, call, false, out var error))
        {
            FailRequest(request, error);
            return;
        }

        var shuttle = request.Shuttle;
        request.PlannedRoster.Clear();
        request.SpawnedGhostRoles.Clear();

        if (!BuildRoster(request, call, out error))
        {
            FailRequest(request, error);
            return;
        }

        if (!SpawnRosterSlots(request, call, shuttle, out error))
        {
            FailRequest(request, error);
            return;
        }

        request.State = RMCERTRequestState.Recruiting;
        request.RecruitmentEndsAt = _timing.CurTime + call.Requirements.RecruitmentDuration;
        request.NextAutoLaunchAttempt = request.RecruitmentEndsAt.Value;
        Log.Info($"ERT request {request.Id} recruiting {request.PlannedRoster.Count} slots for {call.ID}. " +
                 $"RecruitmentDuration={call.Requirements.RecruitmentDuration}, " +
                 $"AutoLaunch={call.AutoLaunch}, MinRequiredSlots={call.Requirements.MinRequiredSlots}, " +
                 $"Shuttle={FormatEntity(shuttle)}, {GetShuttleDiagnostics(request, shuttle)}");
        AddERTRequestLog(LogImpact.Medium,
            "recruiting started",
            request,
            "system",
            call,
            $"slots={request.PlannedRoster.Count}, recruitmentDuration={call.Requirements.RecruitmentDuration.TotalSeconds:0}s, autoLaunch={call.AutoLaunch}");
        _chat.SendAdminAnnouncement(Loc.GetString("rmc-ert-admin-recruiting",
            ("id", request.Id),
            ("slots", request.PlannedRoster.Count),
            ("call", call.Name)));

        if (request.SourceEntity is { Valid: true } beacon &&
            TryComp(beacon, out RMCERTDistressBeaconComponent? beaconComp) &&
            beaconComp.SingleUse)
        {
            beaconComp.Spent = true;
        }

        DirtyState(request);

        // Auto-launch retries are polled from Update instead of chained through Timer.Spawn.
        // TimerManager processes newly-added timers in the same update pass, so adding a short
        // retry timer from a timer callback can snowball if the server has a long frame.
    }

    private bool TryPrepareRequestForDispatch(
        RMCERTRequest request,
        RMCERTCallPrototype call,
        bool approving,
        out string error)
    {
        error = string.Empty;

        if (!TryEnsureShuttleLoaded(request, call, out var shuttle, out var loadedForPreflight, out error))
            return false;

        if (shuttle is not { Valid: true } shuttleUid || !Exists(shuttleUid))
            return true;

        if (!TryFindNavigationComputer(shuttleUid, out var computer))
        {
            error = Loc.GetString("rmc-ert-error-no-navigation-computer");
            Log.Warning($"ERT request {request.Id} preflight failed for {call.ID}: no navigation computer. " +
                        $"Shuttle={FormatEntity(shuttleUid)}, {GetShuttleDiagnostics(request, shuttleUid)}");

            if (loadedForPreflight)
                DiscardPreflightShuttle(request, shuttleUid);

            return false;
        }

        if (!TryFindLandingZone(request, call, computer, out _, out var landingZoneError))
        {
            LogLandingZoneDiagnostics(request, computer);
            error = !string.IsNullOrWhiteSpace(landingZoneError)
                ? landingZoneError
                : approving
                ? Loc.GetString("rmc-ert-warning-no-compatible-landing-zone", ("call", call.Name))
                : Loc.GetString("rmc-ert-error-no-landing-zone");

            if (loadedForPreflight)
                DiscardPreflightShuttle(request, shuttleUid);

            return false;
        }

        return true;
    }

    private bool TryEnsureShuttleLoaded(
        RMCERTRequest request,
        RMCERTCallPrototype call,
        out EntityUid? shuttle,
        out bool loadedForPreflight,
        out string error)
    {
        loadedForPreflight = false;
        error = string.Empty;

        if (request.Shuttle is { Valid: true } existing && Exists(existing))
        {
            shuttle = existing;
            return true;
        }

        if (!TryLoadShuttle(request, call, out shuttle, out error))
            return false;

        request.Shuttle = shuttle;
        loadedForPreflight = shuttle is { Valid: true };
        return true;
    }

    private void DiscardPreflightShuttle(RMCERTRequest request, EntityUid shuttle)
    {
        if (request.Shuttle == shuttle)
            request.Shuttle = null;

        request.ShuttleSpawnMarker = null;
        DeleteReturnDestination(request);

        if (Exists(shuttle))
            QueueDel(shuttle);
    }

    private bool TryLoadShuttle(RMCERTRequest request, RMCERTCallPrototype call, out EntityUid? shuttle, out string error)
    {
        shuttle = null;
        error = string.Empty;

        if (!TryGetShuttleMapPath(call, out var shuttleMap, out error))
            return false;

        if (shuttleMap is not { } shuttleMapPath)
            return true;

        if (TryLoadShuttleAtPlacedMarker(request, call, shuttleMapPath, out shuttle, out error) &&
            shuttle is { } placedShuttle)
        {
            ConfigureLoadedShuttle(request, call, placedShuttle);
            return true;
        }

        if (!string.IsNullOrWhiteSpace(error))
            return false;

        if (GetShuttleSpawnMarkerId(call) is { } spawnMarker)
        {
            Log.Warning($"ERT request {request.Id} found no free placed shuttle spawn marker " +
                        $"{spawnMarker.Id} for {call.ID}; falling back to hidden ERT map spawn.");
        }

        if (_ertMap is not { } targetMap || !_map.MapExists(targetMap))
        {
            _map.CreateMap(out var ertMap);
            _ertMap = ertMap;
            targetMap = ertMap;
        }

        // Keep staged ERT shuttles separated on the hidden map so simultaneous dispatches do not overlap.
        var offset = new Vector2(_loadedShuttles * 50, _loadedShuttles * 50);
        _loadedShuttles++;

        if (!_mapLoader.TryLoadGrid(targetMap, shuttleMapPath, out var result, offset: offset) ||
            result == null)
        {
            error = Loc.GetString("rmc-ert-error-load-shuttle-map", ("map", shuttleMapPath));
            return false;
        }

        shuttle = result.Value;
        ConfigureLoadedShuttle(request, call, shuttle.Value);
        return true;
    }

    private bool TryLoadShuttleAtPlacedMarker(
        RMCERTRequest request,
        RMCERTCallPrototype call,
        ResPath shuttleMapPath,
        out EntityUid? shuttle,
        out string error)
    {
        shuttle = null;
        error = string.Empty;

        if (!TryFindShuttleSpawnMarker(request, call, out var marker, out var markerOffset))
            return false;

        var markerXform = Transform(marker);
        if (markerXform.MapUid == null)
            return false;

        var coordinates = _transform.GetMapCoordinates(marker, markerXform).Offset(markerOffset);
        var rotation = _transform.GetWorldRotation(markerXform);

        var stagingOffset = new Vector2(_loadedShuttles * 50, _loadedShuttles * 50);
        _loadedShuttles++;
        if (!_mapLoader.TryLoadGrid(markerXform.MapID, shuttleMapPath, out var result, offset: stagingOffset) ||
            result == null)
        {
            error = Loc.GetString("rmc-ert-error-load-shuttle-map", ("map", shuttleMapPath));
            return false;
        }

        shuttle = result.Value;
        var shuttleXform = Transform(shuttle.Value);
        _transform.SetCoordinates(shuttle.Value, shuttleXform, new EntityCoordinates(markerXform.MapUid.Value, coordinates.Position), rotation);
        request.ShuttleSpawnMarker = marker;
        Log.Info($"ERT request {request.Id} loaded shuttle {ToPrettyString(shuttle.Value)} for {call.ID} " +
                 $"at placed marker {ToPrettyString(marker)}.");
        return true;
    }

    private bool TryFindShuttleSpawnMarker(
        RMCERTRequest request,
        RMCERTCallPrototype call,
        out EntityUid marker,
        out Vector2 markerOffset)
    {
        marker = default;
        markerOffset = default;
        if (GetShuttleSpawnMarkerId(call) is not { } spawnMarker)
            return false;

        var sourceMap = GetRequestSourceMap(request);

        var sourceCandidates = new List<(EntityUid Marker, Vector2 Offset)>();
        var fallbackCandidates = new List<(EntityUid Marker, Vector2 Offset)>();
        var startPadQuery = EntityQueryEnumerator<RMCERTShuttleStartPadComponent, TransformComponent>();
        while (startPadQuery.MoveNext(out var uid, out var startPad, out var xform))
        {
            if (MetaData(uid).EntityPrototype?.ID != spawnMarker.Id)
                continue;

            if (IsShuttleSpawnMarkerReserved(uid))
                continue;

            var targetCandidates = sourceMap != null && xform.MapUid == sourceMap
                ? sourceCandidates
                : fallbackCandidates;
            targetCandidates.Add((uid, startPad.Offset));
        }

        var query = EntityQueryEnumerator<GridSpawnerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var spawner, out var xform))
        {
            if (spawner.Spawn == null)
                continue;

            if (MetaData(uid).EntityPrototype?.ID != spawnMarker.Id)
                continue;

            if (IsShuttleSpawnMarkerReserved(uid))
                continue;

            var targetCandidates = sourceMap != null && xform.MapUid == sourceMap
                ? sourceCandidates
                : fallbackCandidates;
            targetCandidates.Add((uid, spawner.Offset));
        }

        var availableCandidates = sourceCandidates.Count != 0
            ? sourceCandidates
            : fallbackCandidates;

        if (availableCandidates.Count == 0)
            return false;

        var picked = _random.Pick(availableCandidates);
        marker = picked.Marker;
        markerOffset = picked.Offset;
        return true;
    }

    private static EntProtoId? GetShuttleSpawnMarkerId(RMCERTCallPrototype call)
    {
        return call.ShuttleSpawnMarker ?? call.ShuttleSpawner;
    }

    private bool IsShuttleSpawnMarkerReserved(EntityUid marker)
    {
        foreach (var request in _requests.Values)
        {
            if (request.ShuttleSpawnMarker != marker)
                continue;

            if (IsTerminal(request.State))
            {
                if (request.Shuttle is not { Valid: true } terminalShuttle || !Exists(terminalShuttle))
                    continue;
            }

            return true;
        }

        return false;
    }

    private void ConfigureLoadedShuttle(RMCERTRequest request, RMCERTCallPrototype call, EntityUid shuttle)
    {
        var shuttleComp = EnsureComp<RMCERTShuttleComponent>(shuttle);
        shuttleComp.RequestId = request.Id;
        shuttleComp.Call = call.ID;
        shuttleComp.Organization = GetOrganizationLabel(call);
        shuttleComp.NpcFactions = call.NpcFactions.ToList();
        shuttleComp.IffFaction = call.IffFaction;
        shuttleComp.LandingTags = call.LandingTags.ToList();
        Dirty(shuttle, shuttleComp);

        var restrictedShuttle = EnsureComp<RMCRestrictedShuttleComponent>(shuttle);
        restrictedShuttle.RequestId = request.Id;
        restrictedShuttle.Call = call.ID;

        CreateReturnDestination(request, shuttle);
        SetShuttlePlayerRouteLock(shuttle, null);

        var computerQuery = EntityQueryEnumerator<DropshipNavigationComputerComponent, TransformComponent>();
        while (computerQuery.MoveNext(out var uid, out var computer, out var xform))
        {
            if (xform.GridUid != shuttle)
                continue;

            _dropship.ConfigureRestrictedNavigationComputer((uid, computer),
                !shuttleComp.NoHijack,
                false,
                true,
                call.LandingTags,
                call.DeniedLandingTags);
        }

        var lockedDoors = ApplyShuttleDoorConsoleLocks(shuttle);
        Log.Info($"ERT request {request.Id} loaded shuttle {ToPrettyString(shuttle)} for {call.ID}. " +
                 $"prelaunchLockedDoors:{lockedDoors}, {GetShuttleDiagnostics(request, shuttle)}");
    }

    private int ApplyShuttleDoorConsoleLocks(EntityUid shuttle)
    {
        var locked = 0;
        var enumerator = Transform(shuttle).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            if (!HasComp<DockingComponent>(child) ||
                !HasComp<DoorBoltComponent>(child))
            {
                continue;
            }

            EnsureComp<RMCDropshipDoorConsoleLockComponent>(child);
            _dropship.LockDoor(child);
            locked++;
        }

        return locked;
    }

    private bool TryGetReturnHomeFlight(EntityUid shuttle, RMCERTShuttleComponent shuttleComp, out RMCERTRequest request)
    {
        if (_requests.TryGetValue(shuttleComp.RequestId, out request!) &&
            request.State == RMCERTRequestState.Arrived &&
            request.Shuttle == shuttle &&
            request.ShuttleHomeDestination is { } homeDestination &&
            Exists(homeDestination) &&
            TryComp(shuttle, out DropshipComponent? dropship) &&
            dropship.Destination == homeDestination)
        {
            return true;
        }

        request = default!;
        return false;
    }

    private void ReleasePrelaunchShuttleDoorLocks(EntityUid shuttle)
    {
        var enumerator = Transform(shuttle).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            RemComp<RMCDropshipDoorConsoleLockComponent>(child);
        }
    }

    private void CreateReturnDestination(RMCERTRequest request, EntityUid shuttle)
    {
        DeleteReturnDestination(request);

        if (!Exists(shuttle))
            return;

        var coordinates = _transform.GetMapCoordinates(shuttle);
        if (coordinates.MapId == MapId.Nullspace)
            return;

        var destination = Spawn(GetReturnDestinationPrototype(shuttle), coordinates);
        _transform.SetWorldRotation(destination, _transform.GetWorldRotation(shuttle));
        request.ShuttleHomeDestination = destination;
        request.ShuttleHomeIsFallback = request.ShuttleSpawnMarker == null;

        if (TryComp(destination, out DropshipDestinationComponent? destinationComp))
            _dropship.SetDestinationShip((destination, destinationComp), shuttle);

        _dropship.SetCurrentDestination(shuttle, destination);
    }

    private EntProtoId GetReturnDestinationPrototype(EntityUid shuttle)
    {
        if (!TryFindNavigationComputer(shuttle, out var computer))
            return ERTShuttleReturnDestinationPrototype;

        return computer.Comp.ShuttleDockingClass switch
        {
            RMCShuttleDockingClass.Small => ERTShuttleReturnDestinationSmallPrototype,
            RMCShuttleDockingClass.Big => ERTShuttleReturnDestinationBigPrototype,
            _ => ERTShuttleReturnDestinationPrototype,
        };
    }

    private void DeleteReturnDestination(RMCERTRequest request)
    {
        if (request.ShuttleHomeDestination is { Valid: true } destination && Exists(destination))
        {
            if (request.Shuttle is { Valid: true } shuttle && Exists(shuttle))
                _dropship.ClearCurrentDestinationIf(shuttle, destination);

            QueueDel(destination);
        }

        request.ShuttleHomeDestination = null;
        request.ShuttleHomeIsFallback = false;
        request.ReturnAvailableAt = null;
        request.ReturnRouteUnlocked = false;
    }

    private void SetShuttlePlayerRouteLock(EntityUid shuttle, EntityUid? destination)
    {
        _dropship.SetPlayerRouteLock(shuttle, destination);
    }

    private void ClearShuttlePlayerRouteLock(EntityUid shuttle)
    {
        _dropship.ClearPlayerRouteLock(shuttle);
    }

    private void TryUnlockReturnRoute(RMCERTRequest request)
    {
        if (request.ReturnRouteUnlocked ||
            request.ReturnAvailableAt is not { } returnAvailableAt ||
            _timing.CurTime < returnAvailableAt)
        {
            return;
        }

        if (request.Shuttle is not { Valid: true } shuttle ||
            !Exists(shuttle) ||
            request.ShuttleHomeDestination is not { Valid: true } home ||
            !Exists(home))
        {
            return;
        }

        request.ReturnRouteUnlocked = true;
        SetShuttlePlayerRouteLock(shuttle, home);
    }

    private void BeginReturnDelay(RMCERTRequest request)
    {
        request.ReturnAvailableAt = _timing.CurTime + ReturnDelay;
        request.ReturnRouteUnlocked = false;

        if (request.Shuttle is { Valid: true } shuttle && Exists(shuttle))
            SetShuttlePlayerRouteLock(shuttle, null);
    }

    private bool BuildRoster(RMCERTRequest request, RMCERTCallPrototype call, out string error)
    {
        error = string.Empty;
        // Required slots are guaranteed first, then optional slots are rolled until we reach the final team size.
        var roles = call.Roles
            .OrderByDescending(r => r.Leader)
            .ThenByDescending(r => r.Priority)
            .ToList();
        var countsByRole = new Dictionary<string, int>();
        var minTotal = 0;
        var maxTotal = 0;

        foreach (var role in roles)
        {
            if (!_prototypes.HasIndex<EntityPrototype>(role.GhostRoleEntity))
            {
                error = Loc.GetString("rmc-ert-error-missing-ghost-role",
                    ("entity", role.GhostRoleEntity.Id),
                    ("call", call.Name));
                return false;
            }

            var min = GetRoleMinimumCount(role);
            var max = GetRoleMaximumCount(role);
            countsByRole[role.Id] = 0;
            minTotal += min;
            maxTotal += max;

            for (var i = 0; i < min; i++)
            {
                AddRosterSlot(request, role);
                countsByRole[role.Id]++;
            }
        }

        var targetMin = Math.Max(minTotal, call.Requirements.MinRequiredSlots);
        if (targetMin > maxTotal)
        {
            error = Loc.GetString("rmc-ert-error-min-slots-over-max",
                ("call", call.Name),
                ("required", targetMin),
                ("maximum", maxTotal));
            return false;
        }

        var targetCount = targetMin;
        if (maxTotal > targetMin)
            targetCount = _random.Next(targetMin, maxTotal + 1);

        while (request.PlannedRoster.Count < targetCount)
        {
            var totalWeight = 0;
            foreach (var role in roles)
            {
                var remaining = GetRoleMaximumCount(role) - countsByRole[role.Id];
                if (remaining > 0)
                    totalWeight += remaining;
            }

            if (totalWeight <= 0)
                break;

            var roll = _random.Next(totalWeight);
            RMCERTRoleEntry? selected = null;
            foreach (var role in roles)
            {
                var remaining = GetRoleMaximumCount(role) - countsByRole[role.Id];
                if (remaining <= 0)
                    continue;

                if (roll < remaining)
                {
                    selected = role;
                    break;
                }

                roll -= remaining;
            }

            if (selected == null)
                break;

            AddRosterSlot(request, selected);
            countsByRole[selected.Id]++;
        }

        if (request.PlannedRoster.Count < targetMin)
        {
            error = Loc.GetString("rmc-ert-error-planned-slots-too-low",
                ("planned", request.PlannedRoster.Count),
                ("required", targetMin));
            return false;
        }

        return request.PlannedRoster.Count > 0;
    }

    private bool SpawnRosterSlots(RMCERTRequest request, RMCERTCallPrototype call, EntityUid? shuttle, out string error)
    {
        error = string.Empty;

        foreach (var slot in request.PlannedRoster)
        {
            var coords = GetSpawnCoordinates(request, shuttle, slot);
            var spawned = SpawnResponseMember(request, call, slot, coords);
            TryAssignSeat(spawned, shuttle, slot);
            request.SpawnedGhostRoles.Add(spawned);
        }

        return request.SpawnedGhostRoles.Count > 0;
    }

    private void AddRosterSlot(RMCERTRequest request, RMCERTRoleEntry role)
    {
        request.PlannedRoster.Add(new RMCERTRosterSlot
        {
            RoleId = role.Id,
            RoleName = role.Name,
            GhostRoleEntity = role.GhostRoleEntity,
            Leader = role.Leader,
            Priority = role.Priority,
            RoleTags = role.RoleTags.ToList(),
            SeatTags = role.SeatTags.ToList(),
        });
    }

    private EntityUid SpawnResponseMember(
        RMCERTRequest request,
        RMCERTCallPrototype call,
        RMCERTRosterSlot slot,
        EntityCoordinates coordinates)
    {
        EntityUid spawned;
        if (_prototypes.TryIndex(slot.GhostRoleEntity, out var entityPrototype) &&
            entityPrototype.TryGetComponent<RandomHumanoidSpawnerComponent>(out var spawner, _componentFactory) &&
            !string.IsNullOrWhiteSpace(spawner.SettingsPrototypeId))
        {
            spawned = _randomHumanoid.SpawnRandomHumanoid(spawner.SettingsPrototypeId, coordinates, slot.RoleName);
        }
        else
        {
            spawned = Spawn(slot.GhostRoleEntity, coordinates);
        }

        if (TryComp(spawned, out GhostRoleComponent? ghostRole))
        {
            ghostRole.MindRoles.Clear();
        }

        var member = EnsureComp<RMCERTMemberComponent>(spawned);
        member.RequestId = request.Id;
        member.Call = call.ID;
        member.Role = slot.RoleId;
        member.Team = call.Name;
        Dirty(spawned, member);

        return spawned;
    }

    private bool TryAssignSeat(EntityUid member, EntityUid? shuttle, RMCERTRosterSlot slot)
    {
        if (shuttle is not { Valid: true } shuttleUid)
            return false;

        // Prefer the highest-priority compatible seat so command and specialist slots claim their reserved positions first.
        var bestSeat = EntityUid.Invalid;
        var bestPriority = int.MinValue;
        var query = EntityQueryEnumerator<RMCERTSeatComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var seat, out var xform))
        {
            if (xform.GridUid != shuttleUid)
                continue;

            if (seat.OccupiedBy != null)
                continue;

            var roleMatch = MatchesAny(seat.ReservedRoleTags, slot.RoleTags);
            var seatMatch = MatchesAny(seat.SeatTags, slot.SeatTags);
            if (!roleMatch && !seatMatch)
                continue;

            if (seat.Priority <= bestPriority)
                continue;

            bestSeat = uid;
            bestPriority = seat.Priority;
        }

        if (!bestSeat.Valid)
            return false;

        var bestSeatComp = Comp<RMCERTSeatComponent>(bestSeat);
        bestSeatComp.OccupiedBy = GetNetEntity(member);
        bestSeatComp.ReservationExpires = null;
        Dirty(bestSeat, bestSeatComp);

        var coordinates = Transform(bestSeat).Coordinates;
        _transform.SetCoordinates(member, coordinates);
        _buckle.TryBuckle(member, null, bestSeat, popup: false);
        return true;
    }

    private EntityCoordinates GetSpawnCoordinates(RMCERTRequest request, EntityUid? shuttle, RMCERTRosterSlot slot)
    {
        if (shuttle is { Valid: true } shuttleUid)
        {
            // Match SS13-style landmark behavior by picking randomly among the best compatible spawn markers.
            var matchingSpawns = new List<EntityUid>();
            var highestPriority = int.MinValue;
            var query = EntityQueryEnumerator<RMCERTSpawnPointComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var spawn, out var xform))
            {
                if (xform.GridUid != shuttleUid)
                    continue;

                if (!MatchesAny(spawn.RoleTags, slot.RoleTags) && !MatchesAny(spawn.SeatTags, slot.SeatTags))
                    continue;

                if (spawn.Priority > highestPriority)
                {
                    highestPriority = spawn.Priority;
                    matchingSpawns.Clear();
                }

                if (spawn.Priority == highestPriority)
                    matchingSpawns.Add(uid);
            }

            if (matchingSpawns.Count > 0)
                return Transform(_random.Pick(matchingSpawns)).Coordinates;

            return new EntityCoordinates(shuttleUid, Vector2.Zero);
        }

        if (request.SourceEntity is { Valid: true } source &&
            TryComp(source, out TransformComponent? sourceXform))
        {
            return sourceXform.Coordinates;
        }

        return new EntityCoordinates(EntityUid.Invalid, Vector2.Zero);
    }

    private bool TryGetShuttleMapPath(RMCERTCallPrototype call, out ResPath? shuttleMap, out string error)
    {
        shuttleMap = call.ShuttleMap;
        error = string.Empty;

        if (call.ShuttleSpawner is not { } shuttleSpawner)
            return true;

        if (!_prototypes.TryIndex(shuttleSpawner, out var spawnerPrototype))
        {
            error = Loc.GetString("rmc-ert-error-unknown-shuttle-spawner", ("id", shuttleSpawner.Id));
            return false;
        }

        if (!spawnerPrototype.TryGetComponent<GridSpawnerComponent>(out var gridSpawner, _componentFactory))
        {
            error = Loc.GetString("rmc-ert-error-shuttle-spawner-missing-grid", ("id", shuttleSpawner.Id));
            return false;
        }

        if (shuttleMap is null)
            shuttleMap = gridSpawner.Spawn;

        if (shuttleMap is null)
        {
            error = Loc.GetString("rmc-ert-error-shuttle-spawner-no-map", ("id", shuttleSpawner.Id));
            return false;
        }

        return true;
    }

    private bool TryGetHandheldAvailableCalls(
        Entity<RMCERTDistressBeaconComponent> beacon,
        EntityUid user,
        string reason,
        out List<ProtoId<RMCERTCallPrototype>> calls,
        out string error)
    {
        calls = new List<ProtoId<RMCERTCallPrototype>>();
        error = Loc.GetString("rmc-ert-error-beacon-no-teams");

        var configuredCalls = beacon.Comp.AllowedCalls.Count == 0
            ? _prototypes.EnumeratePrototypes<RMCERTCallPrototype>()
                .Where(c => c.Enabled && c.AllowedSources.Contains(RMCERTRequestSource.Handheld))
                .Select(c => new ProtoId<RMCERTCallPrototype>(c.ID))
                .ToList()
            : beacon.Comp.AllowedCalls.ToList();

        if (configuredCalls.Count == 0)
            return false;

        var request = new RMCERTRequest
        {
            Source = RMCERTRequestSource.Handheld,
            SourceEntity = beacon,
            Requester = user,
            SourceName = Name(beacon),
            RequesterName = Name(user),
            Reason = reason,
            CreatedAt = _timing.CurTime,
        };

        foreach (var callId in configuredCalls)
        {
            if (!_prototypes.TryIndex(callId, out var call))
                continue;

            if (!call.Enabled || !call.AllowedSources.Contains(RMCERTRequestSource.Handheld))
                continue;

            if (!CheckRequirements(request, call, out var requirementError))
            {
                error = requirementError;
                continue;
            }

            calls.Add(callId);
        }

        return calls.Count > 0;
    }

    private bool HasActiveRecruitmentRaffles(RMCERTRequest request)
    {
        foreach (var slot in request.SpawnedGhostRoles)
        {
            if (Exists(slot) && HasComp<GhostRoleRaffleComponent>(slot))
                return true;
        }

        return false;
    }

    private int FinalizeRecruitment(RMCERTRequest request)
    {
        var accepted = 0;
        // Trim unclaimed ghost roles before launch so the minimum-slot check only counts accepted responders.
        for (var i = request.SpawnedGhostRoles.Count - 1; i >= 0; i--)
        {
            var member = request.SpawnedGhostRoles[i];
            if (!Exists(member))
            {
                request.SpawnedGhostRoles.RemoveAt(i);
                continue;
            }

            if (TryComp(member, out GhostRoleComponent? ghostRole) &&
                !ghostRole.Taken)
            {
                QueueDel(member);
                request.SpawnedGhostRoles.RemoveAt(i);
                continue;
            }

            accepted++;
        }

        return accepted;
    }

    private void CleanupRequestContent(RMCERTRequest request, string reason)
    {
        // Failed and cancelled requests need to unwind both the staged roster and any destination reservation held by the shuttle.
        var ghostCoordinates = _gameTicker.GetObserverSpawnPoint();
        var ghostedMembers = 0;
        foreach (var member in request.SpawnedGhostRoles)
        {
            if (!Exists(member))
                continue;

            if (_mind.TryGetMind(member, out var mindId, out var mind))
            {
                if (TryGhostMindForCleanup(request, member, mindId, mind, ghostCoordinates, reason))
                    ghostedMembers++;
            }

            QueueDel(member);
        }

        request.SpawnedGhostRoles.Clear();
        request.PlannedRoster.Clear();

        if (request.Shuttle is { Valid: true } shuttle && Exists(shuttle))
        {
            ReleasePrelaunchShuttleDoorLocks(shuttle);
            ClearShuttlePlayerRouteLock(shuttle);
            RemComp<RMCERTShuttleComponent>(shuttle);
            RemComp<RMCRestrictedShuttleComponent>(shuttle);
            RemComp<RMCExpectedDockComponent>(shuttle);

            if (TryComp(shuttle, out DropshipComponent? dropship) &&
                dropship.Destination is { } destination &&
                TryComp(destination, out DropshipDestinationComponent? destinationComp) &&
                destinationComp.Ship == shuttle)
            {
                _dropship.SetDestinationShip((destination, destinationComp), null);
            }

            var actorsGhosted = TryGhostActorsOffShuttle(request, shuttle, ghostCoordinates, reason, out var actorsOnShuttle, out var ghostedActors);
            var remainingActors = CountActorsOnShuttle(shuttle);
            var unhandledActors = Math.Max(0, actorsOnShuttle - ghostedActors);
            Log.Info($"ERT request {request.Id} cleanup '{reason}' for {ToPrettyString(shuttle)}. " +
                      $"GhostedMembers={ghostedMembers}, ActorsOnShuttle={actorsOnShuttle}, " +
                      $"GhostedActors={ghostedActors}, UnhandledActors={unhandledActors}, RemainingActors={remainingActors}, " +
                      GetShuttleDiagnostics(request, shuttle));

            if (actorsGhosted && unhandledActors == 0)
            {
                QueueDel(shuttle);
                request.Shuttle = null;
                request.ShuttleSpawnMarker = null;
                _chat.SendAdminAnnouncement(Loc.GetString("rmc-ert-admin-cleanup",
                    ("id", request.Id),
                    ("reason", reason)));
            }
            else
            {
                Log.Warning($"ERT request {request.Id} left shuttle {ToPrettyString(shuttle)} in world after cleanup '{reason}' " +
                            $"because {remainingActors} actor(s) could not be ghosted safely.");
                _chat.SendAdminAnnouncement(Loc.GetString("rmc-ert-admin-cleanup-deferred",
                    ("id", request.Id),
                    ("reason", reason),
                    ("actors", remainingActors)));
            }
        }
        else
        {
            request.Shuttle = null;
            request.ShuttleSpawnMarker = null;
        }

        DeleteReturnDestination(request);
    }

    private bool TryGhostActorsOffShuttle(
        RMCERTRequest request,
        EntityUid shuttle,
        EntityCoordinates ghostCoordinates,
        string reason,
        out int actorsOnShuttle,
        out int ghostedActors)
    {
        var actors = new List<EntityUid>();
        var query = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (TerminatingOrDeleted(uid))
                continue;

            if (xform.GridUid != shuttle)
                continue;

            actors.Add(uid);
        }

        actorsOnShuttle = actors.Count;
        ghostedActors = 0;

        if (actors.Count == 0)
            return true;

        foreach (var actor in actors)
        {
            if (!Exists(actor) || TerminatingOrDeleted(actor))
            {
                ghostedActors++;
                continue;
            }

            if (!_mind.TryGetMind(actor, out var mindId, out var mind) &&
                (!TryComp(actor, out ActorComponent? actorComp) ||
                 !_mind.TryGetMind(actorComp.PlayerSession, out mindId, out mind)))
            {
                Log.Warning($"ERT request {request.Id} cleanup '{reason}' found actor {ToPrettyString(actor)} " +
                            $"on shuttle {ToPrettyString(shuttle)} but could not find a mind to ghost.");
                continue;
            }

            if (!TryGhostMindForCleanup(request, actor, mindId, mind, ghostCoordinates, reason))
                continue;

            QueueDel(actor);
            ghostedActors++;
        }

        // QueueDel(actor) is processed later, so an immediate entity query can still see
        // actors that were already safely ghosted and queued for deletion.
        return ghostedActors >= actorsOnShuttle;
    }

    private bool TryGhostMindForCleanup(
        RMCERTRequest request,
        EntityUid actor,
        EntityUid mindId,
        MindComponent mind,
        EntityCoordinates ghostCoordinates,
        string reason)
    {
        var ghost = _ghost.SpawnGhost((mindId, mind), ghostCoordinates, canReturn: false);
        if (ghost == null)
        {
            Log.Warning($"ERT request {request.Id} cleanup '{reason}' failed to ghost " +
                        $"{ToPrettyString(actor)} from cleanup shuttle.");
            return false;
        }

        Log.Info($"ERT request {request.Id} cleanup '{reason}' ghosted {ToPrettyString(actor)} " +
                 $"as {ToPrettyString(ghost.Value)}.");
        return true;
    }

    private int CountActorsOnShuttle(EntityUid shuttle)
    {
        var count = 0;
        var query = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (!TerminatingOrDeleted(uid) && xform.GridUid == shuttle)
                count++;
        }

        return count;
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
        return TryLaunch(id, null, true);
    }

    private bool TryLaunch(Guid id, EntityUid? admin, bool automatic)
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
                    GetAdminActorText(admin),
                    call,
                    $"error=\"{FormatLogValue(request.LastError)}\"");
            }
            DirtyState(request);
            return false;
        }

        var launcher = automatic
            ? Loc.GetString("rmc-ert-launcher-automatic")
            : admin is { Valid: true }
                ? ToPrettyString(admin.Value)
                : Loc.GetString("rmc-ert-admin-actor-server");

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
            if (!_prototypes.TryIndex(id, out var call))
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

    private void FailRequest(RMCERTRequest request, string error, bool announceNoResponse = false)
    {
        var previousState = request.State;
        Log.Warning($"ERT request {request.Id} failed: {error}. " +
                    $"State={request.State}, SelectedCall={request.SelectedCall?.Id}, " +
                    $"Shuttle={FormatEntity(request.Shuttle)}, {GetShuttleDiagnostics(request, request.Shuttle)}");
        request.State = RMCERTRequestState.Failed;
        request.LastError = error;
        request.LastWarning = string.Empty;
        request.RecruitmentEndsAt = null;
        CleanupRequestContent(request, Loc.GetString("rmc-ert-cleanup-reason-failed"));
        UpdateSourceVisual(request, false);
        _chat.SendAdminAnnouncement(Loc.GetString("rmc-ert-admin-failed",
            ("id", request.Id),
            ("error", error)));
        AddERTRequestLog(LogImpact.High,
            "failed",
            request,
            "system",
            extra: $"previousState={previousState}, error=\"{FormatLogValue(error)}\"");

        if (request.SelectedCall is { } callId &&
            _prototypes.TryIndex(callId, out var call) &&
            announceNoResponse)
        {
            AnnounceNoResponse(request, call);
        }

        DirtyState(request);
    }

    private void AnnounceDistressLaunched(RMCERTRequest request, RMCERTCallPrototype call)
    {
        var announcement = new RMCERTStageAnnouncement
        {
            Title = "rmc-ert-announcement-title-priority-alert",
            Message = "rmc-ert-announcement-priority-alert",
            Sound = DistressBeaconSound,
        };

        Announce(announcement, request, call);
    }

    private void AnnounceNoResponse(RMCERTRequest request, RMCERTCallPrototype? call)
    {
        var announcement = new RMCERTStageAnnouncement
        {
            Title = "rmc-ert-announcement-title-distress-beacon",
            Message = "rmc-ert-announcement-distress-no-response",
        };

        if (call != null)
        {
            Announce(announcement, request, call);
            return;
        }

        var title = Loc.GetString(announcement.Title!.Value);
        var message = Loc.GetString(announcement.Message!.Value,
            ("team", Loc.GetString("rmc-ert-response-team-fallback")),
            ("requester", request.RequesterName),
            ("reason", request.Reason));
        AnnounceERTToMarines(title, message);
    }

    private static RMCERTStageAnnouncement CreateDefaultDispatchAnnouncement()
    {
        return new RMCERTStageAnnouncement
        {
            Title = "rmc-ert-announcement-title-distress-beacon",
            Message = "rmc-ert-announcement-distress-dispatch",
            Sound = DistressReceivedSound,
        };
    }

    private void AnnounceDispatch(RMCERTRequest request, RMCERTCallPrototype call)
    {
        Announce(call.Announcements.Dispatch ?? CreateDefaultDispatchAnnouncement(), request, call);
    }

    private void Announce(RMCERTStageAnnouncement? announcement, RMCERTRequest request, RMCERTCallPrototype call)
    {
        if (announcement == null)
            return;

        var message = announcement.Message;
        if (message == null && announcement.Messages.Count > 0)
            message = _random.Pick(announcement.Messages);

        if (message == null)
            return;

        var text = FormatCallText(message.Value, request, call);
        var title = announcement.Title != null
            ? FormatCallText(announcement.Title.Value, request, call)
            : null;

        AnnounceERTToMarines(title, text, announcement.Sound, announcement.RawTitle);
    }

    private void AnnounceERTToMarines(string? title, string message, SoundSpecifier? sound = null, bool rawTitle = true)
    {
        title ??= Loc.GetString("rmc-announcement-author-highcommand");
        var loc = rawTitle
            ? "rmc-ert-announcement-message"
            : "rmc-announcement-message";
        var wrapped = Loc.GetString(loc, ("author", title), ("title", title), ("message", message));
        _marineAnnounce.AnnounceToMarines(wrapped, sound);
    }

    private void AddERTRequestLog(
        LogImpact impact,
        string action,
        RMCERTRequest request,
        string actor,
        RMCERTCallPrototype? call = null,
        string? extra = null)
    {
        var selected = call?.Name ?? GetSelectedCallLabel(request) ?? "none";
        var extraText = string.IsNullOrWhiteSpace(extra)
            ? string.Empty
            : $", {extra}";

        _adminLog.Add(LogType.RMCAdminCommandLogging,
            impact,
            $"ERT {action}: request={request.Id}, actor={FormatLogValue(actor)}, state={request.State}, " +
            $"source={request.Source}, requester={FormatLogValue(GetRequesterLogText(request))}, " +
            $"sourceName={FormatLogValue(request.SourceName)}, sourceEntity={FormatEntity(request.SourceEntity)}, " +
            $"call={FormatLogValue(selected)}, reason=\"{FormatLogValue(request.Reason)}\", " +
            $"shuttle={FormatEntity(request.Shuttle)}{extraText}");
    }

    private string GetRequesterLogText(RMCERTRequest request)
    {
        if (request.Requester is { Valid: true } requester && Exists(requester))
            return ToPrettyString(requester);

        return request.RequesterName;
    }

    private static string FormatLogValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "none";

        return value.Replace('\r', ' ').Replace('\n', ' ');
    }

    private string GetAdminActorText(EntityUid? admin, string? fallbackName = null)
    {
        if (admin is { Valid: true } && Exists(admin.Value))
            return ToPrettyString(admin.Value);

        if (!string.IsNullOrWhiteSpace(fallbackName))
            return fallbackName;

        return Loc.GetString("rmc-ert-admin-actor-server");
    }

    private string GetForceCallAnnouncement(RMCERTRequest request, RMCERTCallPrototype call, string adminText)
    {
        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            return Loc.GetString("rmc-ert-admin-force-called-reason",
                ("admin", adminText),
                ("id", request.Id),
                ("call", call.Name),
                ("delay", (int)call.LaunchDelay),
                ("reason", request.Reason));
        }

        return Loc.GetString("rmc-ert-admin-force-called",
            ("admin", adminText),
            ("id", request.Id),
            ("call", call.Name),
            ("delay", (int)call.LaunchDelay));
    }

    private string BuildAdminRequestAnnouncement(RMCERTRequest request)
    {
        var sourceLabel = GetRequestSourceLabel(request);
        var baseText = Loc.GetString("rmc-ert-admin-request",
            ("id", request.Id),
            ("requester", request.RequesterName),
            ("source", sourceLabel),
            ("reason", request.Reason));
        if (request.AllowedCalls.Count != 1 ||
            !_prototypes.TryIndex(request.AllowedCalls[0], out var call) ||
            call.Announcements.RequestAdmin == null)
        {
            return baseText;
        }

        var extra = FormatCallText(call.Announcements.RequestAdmin.Value, request, call);
        return Loc.GetString("rmc-ert-admin-request-with-extra", ("base", baseText), ("extra", extra));
    }

    private string GetRequestSuccessText(EntityUid sourceEntity, RMCERTRequestSource source)
    {
        if (source == RMCERTRequestSource.Handheld &&
            TryComp(sourceEntity, out RMCERTDistressBeaconComponent? beacon))
        {
            return Loc.GetString("rmc-ert-success-handheld", ("recipient", beacon.Recipient));
        }

        return Loc.GetString("rmc-ert-success-console");
    }

    private string GetRequestSourceLabel(RMCERTRequest request)
    {
        if (request.Source == RMCERTRequestSource.Handheld &&
            request.SourceEntity is { Valid: true } source &&
            TryComp(source, out RMCERTDistressBeaconComponent? beacon))
        {
            return beacon.RequestTitle;
        }

        return RMCERTLoc.GetSource(request.Source);
    }

    private string BuildMemberBriefing(RMCERTRequest request, RMCERTCallPrototype call)
    {
        if (call.Objectives.Count == 0 &&
            call.Features.Count == 0 &&
            string.IsNullOrWhiteSpace(request.Reason))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.AppendLine(Loc.GetString("rmc-ert-briefing-title", ("team", call.Name)));

        if (!string.IsNullOrWhiteSpace(request.Reason))
            builder.AppendLine(Loc.GetString("rmc-ert-briefing-reason", ("reason", request.Reason)));

        if (call.Objectives.Count > 0)
        {
            builder.AppendLine(Loc.GetString("rmc-ert-briefing-objectives"));
            foreach (var objective in call.Objectives)
            {
                builder.Append(Loc.GetString("rmc-ert-briefing-bullet"));
                builder.AppendLine(FormatCallText(objective, request, call));
            }
        }

        if (call.Features.Count > 0)
        {
            builder.AppendLine(Loc.GetString("rmc-ert-briefing-features"));
            foreach (var feature in call.Features)
            {
                builder.Append(Loc.GetString("rmc-ert-briefing-bullet"));
                builder.AppendLine(FormatCallText(feature, request, call));
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatCallText(LocId text, RMCERTRequest request, RMCERTCallPrototype call)
    {
        return Robust.Shared.Localization.Loc.GetString(text,
            ("team", call.Name),
            ("requester", request.RequesterName),
            ("reason", request.Reason));
    }

    private void NotifyAdminsOfRequest()
    {
        if (!_adminManager.ActiveAdmins.Any())
            return;

        _audio.PlayGlobal(
            AdminRequestSound,
            Filter.Empty().AddPlayers(_adminManager.ActiveAdmins),
            false,
            AudioParams.Default.WithVolume(-6f));
    }

    private void MarkArrived(RMCERTRequest request, RMCERTCallPrototype call, string? detail = null)
    {
        request.State = RMCERTRequestState.Arrived;
        request.LastError = string.Empty;
        request.LastWarning = string.Empty;
        BeginReturnDelay(request);
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

    private void UpdateSourceVisual(RMCERTRequest request, bool active)
    {
        if (request.SourceEntity is { Valid: true } source &&
            HasComp<RMCERTDistressBeaconComponent>(source))
        {
            UpdateBeaconVisual(source, active);
        }
    }

    private void UpdateBeaconVisual(EntityUid uid, bool active)
    {
        if (TryComp<AppearanceComponent>(uid, out var appearance))
            _appearance.SetData(uid, RMCERTDistressBeaconVisuals.Active, active, appearance);
    }

    private static int GetRoleMinimumCount(RMCERTRoleEntry role)
    {
        var min = Math.Max(0, role.Min);
        if (role.Required)
            min = Math.Max(min, 1);

        return min;
    }

    private static int GetRoleMaximumCount(RMCERTRoleEntry role)
    {
        var min = GetRoleMinimumCount(role);
        return Math.Max(min, role.Max);
    }

    private void ValidatePrototypes()
    {
        foreach (var call in _prototypes.EnumeratePrototypes<RMCERTCallPrototype>())
        {
            if (call.Roles.Count == 0)
                Log.Error($"ERT call {call.ID} has no roles configured.");

            if (!TryGetShuttleMapPath(call, out _, out var shuttleError))
                Log.Error($"ERT call {call.ID} has invalid shuttle configuration: {shuttleError}");

            if (call.ShuttleSpawnMarker is { } spawnMarker)
            {
                if (!_prototypes.TryIndex(spawnMarker, out var spawnMarkerPrototype))
                {
                    Log.Error($"ERT call {call.ID} references missing shuttle spawn marker {spawnMarker.Id}.");
                }
                else if (!spawnMarkerPrototype.HasComponent<RMCERTShuttleStartPadComponent>(_componentFactory) &&
                         !spawnMarkerPrototype.HasComponent<GridSpawnerComponent>(_componentFactory))
                {
                    Log.Error($"ERT call {call.ID} shuttle spawn marker {spawnMarker.Id} is missing RMCERTShuttleStartPadComponent.");
                }
            }

            if (string.IsNullOrWhiteSpace(call.Organization) &&
                call.NpcFactions.Count == 0 &&
                call.IffFaction == null)
            {
                Log.Error($"ERT call {call.ID} is missing organization, NPC factions, and IFF faction metadata.");
            }

            foreach (var role in call.Roles)
            {
                if (!_prototypes.HasIndex<EntityPrototype>(role.GhostRoleEntity))
                    Log.Error($"ERT call {call.ID} role {role.Id} references missing ghost role entity {role.GhostRoleEntity}.");

                if (role.Max < role.Min)
                    Log.Error($"ERT call {call.ID} role {role.Id} has max {role.Max} lower than min {role.Min}.");
            }

            var totalMax = call.Roles.Sum(GetRoleMaximumCount);
            var requiredMinimum = Math.Max(call.Requirements.MinRequiredSlots, call.Roles.Sum(GetRoleMinimumCount));
            if (requiredMinimum > totalMax)
            {
                Log.Error($"ERT call {call.ID} requires at least {requiredMinimum} roster slots, but only {totalMax} are available.");
            }
        }
    }

    private static string GetOrganizationLabel(RMCERTCallPrototype call)
    {
        if (!string.IsNullOrWhiteSpace(call.Organization))
            return call.Organization;

        if (call.NpcFactions.Count > 0)
            return call.NpcFactions[0].Id;

        if (call.IffFaction is { } iffFaction)
            return iffFaction.Id;

        return call.Name;
    }

    private string? GetSelectedCallLabel(RMCERTRequest request)
    {
        if (request.SelectedCall is not { } callId)
            return null;

        if (_prototypes.TryIndex(callId, out var call))
            return call.Name;

        return callId.Id;
    }

    private string FormatEntity(EntityUid? entity)
    {
        return entity is { Valid: true } uid && Exists(uid)
            ? ToPrettyString(uid)
            : "none";
    }

    private string GetShuttleDiagnostics(EntityUid? shuttle)
    {
        if (shuttle is not { Valid: true } shuttleUid || !Exists(shuttleUid))
            return "ShuttleState=missing";

        var xform = Transform(shuttleUid);
        var mapId = xform.MapID;
        var mapInitialized = _map.MapExists(mapId) && _map.IsInitialized(mapId);
        var mapPaused = _map.MapExists(mapId) && _map.IsPaused(mapId);
        var gridPaused = MetaData(shuttleUid).EntityPaused;
        var navComputers = CountNavigationComputers(shuttleUid);
        var actors = CountActorsOnShuttle(shuttleUid);

        return $"ShuttleState=map:{mapId}, mapInit:{mapInitialized}, mapPaused:{mapPaused}, " +
               $"gridPaused:{gridPaused}, navComputers:{navComputers}, actors:{actors}";
    }

    private string GetShuttleDiagnostics(RMCERTRequest request, EntityUid? shuttle)
    {
        var diagnostics = GetShuttleDiagnostics(shuttle);
        if (shuttle is not { Valid: true } shuttleUid || !Exists(shuttleUid))
            return diagnostics;

        var landingZones = 0;
        if (TryFindNavigationComputer(shuttleUid, out var computer))
            landingZones = CountLandingZoneCandidates(request, computer);

        return $"{diagnostics}, spawnMarker:{FormatEntity(request.ShuttleSpawnMarker)}, landingZones:{landingZones}";
    }

    private int CountNavigationComputers(EntityUid shuttle)
    {
        var count = 0;
        var query = EntityQueryEnumerator<DropshipNavigationComputerComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid == shuttle)
                count++;
        }

        return count;
    }

    private int CountLandingZoneCandidates(
        RMCERTRequest request,
        Entity<DropshipNavigationComputerComponent> computer)
    {
        var count = 0;
        var sourceMap = GetRequestSourceMap(request);

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

            count++;
        }

        return count;
    }

    private void LogLandingZoneDiagnostics(
        RMCERTRequest request,
        Entity<DropshipNavigationComputerComponent> computer)
    {
        var sourceMap = GetRequestSourceMap(request);

        var computerXform = Transform(computer);
        var builder = new StringBuilder();
        builder.Append($"ERT request {request.Id} has no valid landing zone. ");
        builder.Append($"call:{request.SelectedCall?.Id ?? "none"}, ");
        builder.Append($"source:{FormatEntity(request.SourceEntity)}, ");
        builder.Append($"sourceMap:{FormatEntity(sourceMap)}, ");
        builder.Append($"computer:{ToPrettyString(computer.Owner)}, ");
        builder.Append($"computerMap:{FormatEntity(computerXform.MapUid)}, ");
        builder.Append($"shuttle:{FormatEntity(computerXform.GridUid)}, ");
        builder.Append($"class:{computer.Comp.ShuttleDockingClass}, ");
        builder.Append($"bounds:{FormatNullable(computer.Comp.DockingBounds)}, ");
        builder.Append($"allowedTags:[{string.Join(", ", computer.Comp.AllowedLandingTags)}], ");
        builder.Append($"deniedTags:[{string.Join(", ", computer.Comp.DeniedLandingTags)}]");

        var total = 0;
        var accepted = 0;
        var rejected = 0;
        var query = EntityQueryEnumerator<DropshipDestinationComponent>();
        while (query.MoveNext(out var uid, out var dropshipDestination))
        {
            total++;
            var reasons = new List<string>();
            var destinationXform = Transform(uid);
            if (dropshipDestination.Ship is { } occupiedBy)
                reasons.Add($"occupiedBy={FormatEntity(occupiedBy)}");

            if (sourceMap != null && destinationXform.MapUid != sourceMap)
                reasons.Add($"mapMismatch destinationMap={FormatEntity(destinationXform.MapUid)} sourceMap={FormatEntity(sourceMap)}");

            if (!_dropship.CanUseDestinationForShuttle(computer, uid, out var reason))
                reasons.Add($"dropshipRule={reason}");

            if (reasons.Count == 0)
            {
                accepted++;
                continue;
            }

            rejected++;
            var meta = MetaData(uid);
            var prototype = meta.EntityPrototype?.ID ?? "none";
            var landingPolicy = $"destination enabled:{dropshipDestination.Enabled} reserved:{dropshipDestination.Reserved} " +
                                $"classes:[{string.Join(", ", dropshipDestination.LandingClasses)}] " +
                                $"tags:[{string.Join(", ", dropshipDestination.LandingTags)}]";

            builder.AppendLine();
            builder.Append($" - rejected {ToPrettyString(uid)} proto:{prototype} map:{FormatEntity(destinationXform.MapUid)} ");
            builder.Append($"dockBounds:{FormatNullable(dropshipDestination.DockBounds)} ship:{FormatEntity(dropshipDestination.Ship)} ");
            builder.Append($"{landingPolicy} reason:{string.Join("; ", reasons)}");
        }

        builder.AppendLine();
        builder.Append($"Landing zone totals: total:{total}, accepted:{accepted}, rejected:{rejected}");
        Log.Warning(builder.ToString());
    }

    private static string FormatNullable(object? value)
    {
        return value?.ToString() ?? "null";
    }

    private void DirtyState()
    {
        var ev = new RMCERTStateChangedEvent();
        RaiseLocalEvent(ref ev);
    }

    private void DirtyState(RMCERTRequest request)
    {
        DirtyState();
        _discordWebhook.SyncRequest(request);
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

    private static string FormatRoundTime(TimeSpan time)
    {
        return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
    }
}
