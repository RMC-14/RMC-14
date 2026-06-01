using System.Linq;
using System.Numerics;
using Content.Server._RMC14.GameStates;
using Content.Server._RMC14.Marines;
using Content.Server._RMC14.Shuttles;
using Content.Server.Doors.Systems;
using Content.Server.GameTicking;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.Utility.Components;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Thunderdome;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared.Administration.Logs;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Timing;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Dropship;

public sealed class DropshipSystem : SharedDropshipSystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DoorSystem _door = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedXenoAnnounceSystem _xenoAnnounce = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _rmcFlammable = default!;
    [Dependency] private readonly SharedRMCExplosionSystem _rmcExplosion = default!;
    [Dependency] private readonly RMCPvsSystem _rmcPvs = default!;
    [Dependency] private readonly RMCAlertLevelSystem _alertLevelSystem = default!;
    [Dependency] private readonly AreaSystem _area = default!;

    private EntityQuery<DockingComponent> _dockingQuery;
    private EntityQuery<DoorComponent> _doorQuery;
    private EntityQuery<DoorBoltComponent> _doorBoltQuery;

    private TimeSpan _lzPrimaryAutoDelay;
    private TimeSpan _flyByTime;
    private TimeSpan _hijackTravelTime;
    private TimeSpan _autopilotDefaultDelay;
    private TimeSpan _autopilotMinDelay;
    private TimeSpan _autopilotMaxDelay;
    private TimeSpan _autopilotRetryDelay;
    private TimeSpan _nextAutopilotUiRefresh;

    private EntityUid _dropshipId;
    private bool _hijack;

    private const float DepartureLocationSearchRange = 12;

    public override void Initialize()
    {
        base.Initialize();

        _dockingQuery = GetEntityQuery<DockingComponent>();
        _doorQuery = GetEntityQuery<DoorComponent>();
        _doorBoltQuery = GetEntityQuery<DoorBoltComponent>();

        SubscribeLocalEvent<DropshipNavigationComputerComponent, DropshipLockoutDoAfterEvent>(OnNavigationLockout);
        SubscribeLocalEvent<DropshipRemoteControlConsoleComponent, ActivateInWorldEvent>(OnRemoteControlActivateInWorld, before: [typeof(ActivatableUISystem), typeof(ActivatableUIRequiresAccessSystem)]);
        SubscribeLocalEvent<DropshipRemoteControlConsoleComponent, ActivatableUIOpenAttemptEvent>(OnRemoteControlOpenAttempt);
        SubscribeLocalEvent<DropshipRemoteControlConsoleComponent, AfterActivatableUIOpenEvent>(OnRemoteControlConsoleOpen);

        SubscribeLocalEvent<DropshipComponent, FTLRequestEvent>(OnFtlRequested);
        SubscribeLocalEvent<DropshipComponent, FTLStartedEvent>(OnFTLStarted);
        SubscribeLocalEvent<DropshipComponent, FTLCompletedEvent>(OnFTLCompleted);
        SubscribeLocalEvent<DropshipComponent, FTLUpdatedEvent>(OnFTLUpdated);
        SubscribeLocalEvent<DropshipComponent, BeforeFTLStartedEvent>(OnBeforeFTLStarted);

        SubscribeLocalEvent<DropshipInFlyByComponent, FTLCompletedEvent>(OnInFlyByFTLCompleted);

        SubscribeLocalEvent<DropshipDestinationComponent, DropshipRelayedEvent<FTLStartedEvent>>(OnDepartureLocationFTLStarted);
        SubscribeLocalEvent<DropshipDestinationComponent, DropshipRelayedEvent<FTLCompletedEvent>>(OnDestinationLocationFTLCompleted);
        SubscribeLocalEvent<DropshipDestinationComponent, DropshipRelayedEvent<FTLUpdatedEvent>>(OnDestinationLocationFTLUpdated);

        Subs.BuiEvents<DropshipNavigationComputerComponent>(DropshipNavigationUiKey.Key,
            subs =>
            {
                subs.Event<DropshipLockdownMsg>(OnDropshipNavigationLockdownMsg);
                subs.Event<DropshipLaunchAlarmToggleMsg>(OnDropshipLaunchAlarmToggleMsg);
                subs.Event<DropshipNavigationAutopilotDisableMsg>(OnDropshipNavigationAutopilotDisableMsg);
            });

        Subs.BuiEvents<DropshipRemoteControlConsoleComponent>(DropshipRemoteControlUiKey.Key,
            subs =>
            {
                subs.Event<DropshipRemoteLaunchMsg>(OnDropshipRemoteLaunchMsg);
                subs.Event<DropshipRemoteAutopilotConfigureMsg>(OnDropshipRemoteAutopilotConfigureMsg);
                subs.Event<DropshipRemoteAutopilotDisableMsg>(OnDropshipRemoteAutopilotDisableMsg);
                subs.Event<DropshipRemoteAutopilotLaunchNowMsg>(OnDropshipRemoteAutopilotLaunchNowMsg);
                subs.Event<DropshipRemoteAutopilotRecallNowMsg>(OnDropshipRemoteAutopilotRecallNowMsg);
            });

        Subs.CVar(_config, RMCCVars.RMCLandingZonePrimaryAutoMinutes, v => _lzPrimaryAutoDelay = TimeSpan.FromMinutes(v), true);
        Subs.CVar(_config, RMCCVars.RMCDropshipFlyByTimeSeconds, v => _flyByTime = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_config, RMCCVars.RMCDropshipHijackTravelTimeSeconds, v => _hijackTravelTime = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_config, RMCCVars.RMCDropshipAutopilotDefaultDelaySeconds, v => _autopilotDefaultDelay = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_config, RMCCVars.RMCDropshipAutopilotMinDelaySeconds, v => _autopilotMinDelay = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_config, RMCCVars.RMCDropshipAutopilotMaxDelaySeconds, v => _autopilotMaxDelay = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_config, RMCCVars.RMCDropshipAutopilotRetrySeconds, v => _autopilotRetryDelay = TimeSpan.FromSeconds(v), true);
    }

    private void OnFTLStarted(Entity<DropshipComponent> ent, ref FTLStartedEvent args)
    {
        OnRefreshUI(ent, ref args);

        var map = args.FromMapUid;
        if (HasComp<AlmayerComponent>(map))
        {
            var ev = new DropshipLaunchedFromWarshipEvent(ent);
            RaiseLocalEvent(ent, ref ev, true);
        }

        RelayToMountedEntities(ent, args);
        RelayToDropshipDepartureLocation(ent, args);

        if (!_hijack) // TODO RMC14: Check for locked dropship by queen and friendliness of xenos onboard
        {
            int xenoCount = 0;
            string dropshipName = string.Empty;
            var dropship = EnsureComp<DropshipComponent>(_dropshipId);
            var xenoQuery = EntityQueryEnumerator<XenoComponent, MobStateComponent, TransformComponent>();
            while (xenoQuery.MoveNext(out var uid, out _, out var mobState, out var xform))
            {
                if (xform.GridUid == _dropshipId && mobState.CurrentState != MobState.Dead)
                {
                    xenoCount++;
                    if (string.IsNullOrEmpty(dropshipName) && _area.TryGetArea(uid, out _, out var areaProto))
                        dropshipName = areaProto.Name;
                }
            }

            if (xenoCount > 0)
            {
                _alertLevelSystem.Set(RMCAlertLevels.Red, _dropshipId, false, false);
                _marineAnnounce.AnnounceToMarines(Loc.GetString("rmc-announcement-unidentified-lifesigns",
                    ("name", dropshipName),
                    ("count", xenoCount)),
                    dropship.UnidentifledlifesignsSound);
            }
        }
    }

    private void OnFTLCompleted(Entity<DropshipComponent> ent, ref FTLCompletedEvent args)
    {
        if (ent.Comp.RechargeTime is { } rechargeTime && TryComp(ent, out FTLComponent? ftl))
            ftl.StateTime = StartEndTime.FromCurTime(_timing, rechargeTime);

        OnRefreshUI(ent, ref args);

        var map = args.MapUid;
        if (HasComp<RMCPlanetComponent>(map))
        {
            var ev = new DropshipLandedOnPlanetEvent(ent);
            RaiseLocalEvent(ref ev);
        }

        if (HasComp<AlmayerComponent>(map) && ent.Comp.Crashed)
        {
            var ev = new DropshipHijackLandedEvent(map);
            RaiseLocalEvent(ref ev);
        }

        RelayToMountedEntities(ent, args);
        RelayToDropshipDestination(ent, args);

        ent.Comp.DepartureLocation = ent.Comp.Destination;
        Dirty(ent);
    }

    private void OnFTLUpdated(Entity<DropshipComponent> ent, ref FTLUpdatedEvent args)
    {
        if (TryComp(ent, out FTLComponent? ftl))
        {
            ent.Comp.State = ftl.State;
            Dirty(ent);

            if (ftl.State == FTLState.Starting && ent.Comp.LaunchAlarmEntity != null)
                TryStopLaunchAlarm(ent);
        }

        RefreshUI();
        RefreshRemoteControlConsoles();
    }

    private void OnBeforeFTLStarted(Entity<DropshipComponent> ent, ref BeforeFTLStartedEvent args)
    {
        RelayToMountedEntities(ent, args);
    }

    private void OnRefreshUI<T>(Entity<DropshipComponent> ent, ref T args)
    {
        RefreshUI();
        RefreshRemoteControlConsoles();
    }

    private void OnFtlRequested<T>(Entity<DropshipComponent> ent, ref T args)
    {
        OnRefreshUI(ent, ref args);

        var departureLocations = _entityLookup.GetEntitiesInRange<DropshipDestinationComponent>(ent.Owner.ToCoordinates(), DepartureLocationSearchRange);

        if (departureLocations.Count <= 0)
            return;

        ent.Comp.DepartureLocation = departureLocations.FirstOrDefault();
        Dirty(ent);

        ToggleLandingLights(ent.Comp.DepartureLocation.Value, true);
    }

    private void OnInFlyByFTLCompleted(Entity<DropshipInFlyByComponent> ent, ref FTLCompletedEvent args)
    {
        RemCompDeferred<DropshipInFlyByComponent>(ent);
    }

    private void OnDropshipNavigationLockdownMsg(Entity<DropshipNavigationComputerComponent> ent, ref DropshipLockdownMsg args)
    {
        if (_transform.GetGrid(ent.Owner) is not { } grid ||
            !TryComp(grid, out DropshipComponent? dropship) ||
            dropship.Crashed)
        {
            return;
        }

        if (TryComp(grid, out FTLComponent? ftl) &&
            ftl.State is FTLState.Travelling or FTLState.Arriving &&
            args.DoorLocation != DoorLocation.Aft)
        {
            return;
        }

        dropship.LastLocked.TryGetValue(args.DoorLocation, out var lastLocked);
        var time = _timing.CurTime;
        if (time < lastLocked + dropship.LockCooldown)
            return;

        if (!dropship.LastLocked.TryAdd(args.DoorLocation, time))
            dropship.LastLocked[args.DoorLocation] = time;
        Dirty(grid, dropship);

        SetDocks(grid, args.DoorLocation);
        OnRefreshUI((grid, dropship), ref args);
    }

    private void OnDropshipLaunchAlarmToggleMsg(Entity<DropshipNavigationComputerComponent> ent, ref DropshipLaunchAlarmToggleMsg args)
    {
        if (!TryGetGridDropship(ent, out var dropship))
            return;

        if (TryComp(dropship, out FTLComponent? ftl) &&
            ftl.State is FTLState.Travelling or FTLState.Arriving or FTLState.Starting)
        {
            return;
        }

        if (dropship.Comp.LaunchAlarmEntity != null)
        {
            TryStopLaunchAlarm(dropship, ent.Comp);
        }
        else
        {
            var sound = Audio.PlayPvs(dropship.Comp.LaunchAlarmSound, dropship);
            if (sound == null)
                return;

            _rmcPvs.AddGlobalOverride(sound.Value.Entity);
            dropship.Comp.LaunchAlarmEntity = sound.Value.Entity;
            ent.Comp.LaunchAlarmStatus = true;
            Dirty(ent);
        }

        RefreshUI();
    }

    private void OnNavigationLockout(Entity<DropshipNavigationComputerComponent> ent, ref DropshipLockoutDoAfterEvent args)
    {
        ent.Comp.LockedOutUntil = _timing.CurTime + ent.Comp.LockoutDuration;
        ent.Comp.RemoteControl = false;
        Dirty(ent);

        _ui.CloseUis(ent.Owner);
        UnlockAllDoors(ent);

        _popup.PopupEntity(Loc.GetString("rmc-dropship-locked", ("minutes", (int)ent.Comp.LockoutDuration.TotalMinutes)), ent, args.User, PopupType.Medium);
    }

    private void OnRemoteControlActivateInWorld(Entity<DropshipRemoteControlConsoleComponent> ent, ref ActivateInWorldEvent args)
    {
        var user = args.User;
        if (!HasComp<XenoComponent>(user))
            return;

        args.Handled = true;

        if (!ent.Comp.AllowQuickSummon)
        {
            _popup.PopupEntity(Loc.GetString("rmc-dropship-remote-xeno-clueless", ("console", ent.Owner)), user, user);
            return;
        }

        if (!HasComp<DropshipHijackerComponent>(user))
        {
            _popup.PopupEntity(Loc.GetString("rmc-dropship-remote-xeno-clueless", ("console", ent.Owner)), user, user);
            return;
        }

        if (!TryDropshipLaunchPopup(ent, user, false))
            return;

        if (!TryDropshipHijackPopup(ent, user, false))
            return;

        if (!TryResolveRemoteLinkedLandingZone(ent, out var landingZone))
        {
            _popup.PopupEntity(Loc.GetString("rmc-dropship-remote-error-no-lz"), user, user, PopupType.MediumCaution);
            return;
        }

        if (landingZone.Comp.Ship != null)
        {
            _popup.PopupEntity(Loc.GetString("rmc-dropship-remote-error-destination-occupied"), user, user, PopupType.MediumCaution);
            return;
        }

        if (Count<PrimaryLandingZoneComponent>() > 0 &&
            !HasComp<PrimaryLandingZoneComponent>(landingZone))
        {
            _popup.PopupEntity(Loc.GetString("rmc-dropship-remote-error-not-primary"), user, user, PopupType.MediumCaution);
            return;
        }

        if (TrySummonAnyAvailableDropship(user, landingZone))
        {
            _popup.PopupEntity(Loc.GetString("rmc-dropship-remote-xeno-called"), user, user, PopupType.LargeCaution);
            return;
        }

        _popup.PopupEntity(Loc.GetString("rmc-dropship-remote-error-no-dropships"), user, user, PopupType.LargeCaution);
    }

    private void OnRemoteControlOpenAttempt(Entity<DropshipRemoteControlConsoleComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (HasComp<XenoComponent>(args.User))
            args.Cancel();
    }

    private void OnRemoteControlConsoleOpen(Entity<DropshipRemoteControlConsoleComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        if (!_ui.IsUiOpen(ent.Owner, DropshipRemoteControlUiKey.Key, args.Actor))
            return;

        RefreshRemoteControlConsole(ent);
    }

    private void OnDropshipNavigationAutopilotDisableMsg(Entity<DropshipNavigationComputerComponent> ent, ref DropshipNavigationAutopilotDisableMsg args)
    {
        if (!TryGetGridDropship(ent, out var dropship) ||
            !TryComp(dropship, out DropshipAutopilotComponent? autopilot))
        {
            return;
        }

        SetAutopilotDisabled(dropship, autopilot, args.Actor, Loc.GetString("rmc-dropship-autopilot-status-detail-disabled-flight-computer"));
        RefreshUI();
        RefreshRemoteControlConsoles();
    }

    private void OnDropshipRemoteLaunchMsg(Entity<DropshipRemoteControlConsoleComponent> console, ref DropshipRemoteLaunchMsg args)
    {
        if (!_ui.IsUiOpen(console.Owner, DropshipRemoteControlUiKey.Key, args.Actor))
            return;

        if (!TryGetAutopilotDropship(args.Computer, out var computer, out var dropship))
        {
            Log.Warning($"{ToPrettyString(args.Actor)} tried to remotely pilot an invalid dropship computer {args.Computer}");
            return;
        }

        if (!TryGetEntity(args.Destination, out var destination) ||
            !TryComp(destination, out DropshipDestinationComponent? destinationComp))
        {
            _popup.PopupEntity(Loc.GetString("rmc-dropship-remote-error-invalid-destination"), console, args.Actor, PopupType.MediumCaution);
            return;
        }

        if (!CanRemoteLaunchTo(computer, dropship, (destination.Value, destinationComp), out var reason))
        {
            _popup.PopupEntity(reason, console, args.Actor, PopupType.MediumCaution);
            RefreshRemoteControlConsoles();
            return;
        }

        if (!FlyTo(computer, destination.Value, args.Actor, source: DropshipLaunchSource.RemoteNavigation))
        {
            _popup.PopupEntity(Loc.GetString("rmc-dropship-remote-error-busy"), console, args.Actor, PopupType.MediumCaution);
            RefreshRemoteControlConsoles();
            return;
        }

        _popup.PopupEntity(Loc.GetString("rmc-dropship-remote-launched", ("destination", Name(destination.Value))), console, args.Actor, PopupType.Medium);
        RefreshUI(computer);
        RefreshRemoteControlConsoles();
    }

    private void OnDropshipRemoteAutopilotConfigureMsg(Entity<DropshipRemoteControlConsoleComponent> console, ref DropshipRemoteAutopilotConfigureMsg args)
    {
        if (!_ui.IsUiOpen(console.Owner, DropshipRemoteControlUiKey.Key, args.Actor))
            return;

        if (!TryGetAutopilotDropship(args.Computer, out var computer, out var dropship))
        {
            Log.Warning($"{ToPrettyString(args.Actor)} tried to configure autopilot for invalid dropship computer {args.Computer}");
            return;
        }

        var autopilot = EnsureComp<DropshipAutopilotComponent>(dropship);
        autopilot.Delay = ClampAutopilotDelay(TimeSpan.FromSeconds(args.DelaySeconds));

        if (args.Mode == DropshipAutopilotMode.Disabled)
        {
            SetAutopilotDisabled(dropship, autopilot, args.Actor, Loc.GetString("rmc-dropship-autopilot-status-detail-disabled-remote"));
            RefreshUI(computer);
            RefreshRemoteControlConsoles();
            return;
        }

        if (args.RouteHangar == null ||
            !TryGetEntity(args.RouteHangar.Value, out var routeHangar) ||
            !TryComp(routeHangar, out DropshipDestinationComponent? routeHangarDestination) ||
            !CanUseDestinationForRemoteOrAutopilot(
                computer,
                dropship,
                (routeHangar.Value, routeHangarDestination),
                DropshipDestinationKind.Hangar,
                false,
                out _))
        {
            autopilot.Mode = DropshipAutopilotMode.Disabled;
            autopilot.NextDepartureAt = null;
            autopilot.RetryAt = null;
            SetAutopilotStatus(dropship, autopilot, DropshipAutopilotStatus.Error, Loc.GetString("rmc-dropship-autopilot-status-detail-select-hangar"));
            _popup.PopupEntity(Loc.GetString("rmc-dropship-autopilot-error-no-home"), console, args.Actor, PopupType.MediumCaution);
            RefreshUI(computer);
            RefreshRemoteControlConsoles();
            return;
        }

        if (IsDestinationOccupiedByOther(dropship, routeHangarDestination))
        {
            SetAutopilotStatus(dropship, autopilot, DropshipAutopilotStatus.Blocked, Loc.GetString("rmc-dropship-autopilot-status-detail-hangar-occupied"));
            _popup.PopupEntity(Loc.GetString("rmc-dropship-autopilot-error-hangar-occupied"), console, args.Actor, PopupType.MediumCaution);
            RefreshRemoteControlConsoles();
            return;
        }

        if (args.LandingZone == null ||
            !TryGetEntity(args.LandingZone.Value, out var lz) ||
            !TryComp(lz, out DropshipDestinationComponent? lzDestination) ||
            !CanUseDestinationForRemoteOrAutopilot(
                computer,
                dropship,
                (lz.Value, lzDestination),
                DropshipDestinationKind.LandingZone,
                false,
                out _))
        {
            autopilot.Mode = DropshipAutopilotMode.Disabled;
            autopilot.NextDepartureAt = null;
            autopilot.RetryAt = null;
            SetAutopilotStatus(dropship, autopilot, DropshipAutopilotStatus.Error, Loc.GetString("rmc-dropship-autopilot-status-detail-select-lz"));
            _popup.PopupEntity(Loc.GetString("rmc-dropship-autopilot-error-invalid-lz"), console, args.Actor, PopupType.MediumCaution);
            RefreshUI(computer);
            RefreshRemoteControlConsoles();
            return;
        }

        if (IsDestinationOccupiedByOther(dropship, lzDestination))
        {
            SetAutopilotStatus(dropship, autopilot, DropshipAutopilotStatus.Blocked, Loc.GetString("rmc-dropship-autopilot-status-detail-lz-occupied"));
            _popup.PopupEntity(Loc.GetString("rmc-dropship-autopilot-error-occupied"), console, args.Actor, PopupType.MediumCaution);
            RefreshRemoteControlConsoles();
            return;
        }

        autopilot.RouteHangar = routeHangar.Value;
        autopilot.LandingZone = lz.Value;
        autopilot.Mode = args.Mode;
        autopilot.NextDepartureAt = null;
        autopilot.RetryAt = null;
        SetAutopilotStatus(dropship, autopilot, DropshipAutopilotStatus.Ready, Loc.GetString("rmc-dropship-autopilot-status-detail-route-armed"));

        _adminLog.Add(LogType.RMCDropshipAutopilot,
            $"{ToPrettyString(args.Actor):player} configured autopilot on {ToPrettyString(dropship):dropship} for {args.Mode} between {ToPrettyString(routeHangar):hangar} and {ToPrettyString(lz):landingzone}");

        RefreshUI(computer);
        RefreshRemoteControlConsoles();
    }

    private void OnDropshipRemoteAutopilotDisableMsg(Entity<DropshipRemoteControlConsoleComponent> console, ref DropshipRemoteAutopilotDisableMsg args)
    {
        if (!_ui.IsUiOpen(console.Owner, DropshipRemoteControlUiKey.Key, args.Actor))
            return;

        if (!TryGetAutopilotDropship(args.Computer, out var computer, out var dropship))
            return;

        if (TryComp(dropship, out DropshipAutopilotComponent? autopilot))
            SetAutopilotDisabled(dropship, autopilot, args.Actor, Loc.GetString("rmc-dropship-autopilot-status-detail-disabled-remote"));

        RefreshUI(computer);
        RefreshRemoteControlConsoles();
    }

    private void OnDropshipRemoteAutopilotLaunchNowMsg(Entity<DropshipRemoteControlConsoleComponent> console, ref DropshipRemoteAutopilotLaunchNowMsg args)
    {
        if (!_ui.IsUiOpen(console.Owner, DropshipRemoteControlUiKey.Key, args.Actor))
            return;

        if (!TryGetAutopilotDropship(args.Computer, out var computer, out var dropship) ||
            !TryComp(dropship, out DropshipAutopilotComponent? autopilot))
        {
            return;
        }

        if (!TryGetNextAutopilotTarget(dropship, autopilot, out var target, out var reason))
        {
            var holdingAtRouteHangar = Loc.GetString("rmc-dropship-autopilot-status-detail-holding-hangar");
            if (reason == holdingAtRouteHangar)
            {
                autopilot.NextDepartureAt = null;
                autopilot.RetryAt = null;
                Dirty(dropship.Owner, autopilot);
                SetAutopilotStatus(dropship, autopilot, DropshipAutopilotStatus.Ready, reason);
            }
            else
            {
                SetAutopilotStatus(dropship, autopilot, DropshipAutopilotStatus.Error, reason);
            }

            RefreshRemoteControlConsoles();
            return;
        }

        TryLaunchAutopilotNow(dropship, autopilot, computer, target);
        RefreshUI(computer);
        RefreshRemoteControlConsoles();
    }

    private void OnDropshipRemoteAutopilotRecallNowMsg(Entity<DropshipRemoteControlConsoleComponent> console, ref DropshipRemoteAutopilotRecallNowMsg args)
    {
        if (!_ui.IsUiOpen(console.Owner, DropshipRemoteControlUiKey.Key, args.Actor))
            return;

        if (!TryGetAutopilotDropship(args.Computer, out var computer, out var dropship) ||
            !TryComp(dropship, out DropshipAutopilotComponent? autopilot))
        {
            return;
        }

        if (!TryResolveRouteHangar(dropship, autopilot, out var routeHangar))
        {
            SetAutopilotStatus(dropship, autopilot, DropshipAutopilotStatus.Error, Loc.GetString("rmc-dropship-autopilot-status-detail-select-hangar"));
            RefreshRemoteControlConsoles();
            return;
        }

        if (dropship.Comp.Destination == routeHangar)
        {
            autopilot.NextDepartureAt = null;
            autopilot.RetryAt = null;
            SetAutopilotStatus(dropship, autopilot, DropshipAutopilotStatus.Ready, Loc.GetString("rmc-dropship-autopilot-status-detail-holding-hangar"));
        }
        else
        {
            TryLaunchAutopilotNow(dropship, autopilot, computer, routeHangar);
        }

        RefreshUI(computer);
        RefreshRemoteControlConsoles();
    }

    private bool TryResolveRemoteLinkedLandingZone(
        Entity<DropshipRemoteControlConsoleComponent> console,
        out Entity<DropshipDestinationComponent> landingZone)
    {
        if (console.Comp.LinkedLandingZone is { } linked &&
            !TerminatingOrDeleted(linked) &&
            TryComp(linked, out DropshipDestinationComponent? linkedDestination) &&
            ResolveDestinationKind((linked, linkedDestination)) == DropshipDestinationKind.LandingZone)
        {
            landingZone = (linked, linkedDestination);
            return true;
        }

        if (FindClosestRemoteLandingZone(console.Owner) is { } closest)
        {
            landingZone = (closest.Owner, closest.Comp);
            return true;
        }

        landingZone = default;
        return false;
    }

    private Entity<DropshipDestinationComponent>? FindClosestRemoteLandingZone(EntityUid console)
    {
        if (!TryComp(console, out TransformComponent? consoleTransform))
            return null;

        Entity<DropshipDestinationComponent>? closestLandingZone = null;
        float closestDistance = 0;
        var destinations = EntityQueryEnumerator<DropshipDestinationComponent, TransformComponent>();
        while (destinations.MoveNext(out var uid, out var destination, out var transform))
        {
            if (transform.MapID != consoleTransform.MapID ||
                ResolveDestinationKind((uid, destination)) != DropshipDestinationKind.LandingZone ||
                !consoleTransform.Coordinates.TryDistance(EntityManager, transform.Coordinates, out var distance))
            {
                continue;
            }

            if (closestLandingZone == null || distance < closestDistance)
            {
                closestLandingZone = (uid, destination);
                closestDistance = distance;
            }
        }

        return closestLandingZone;
    }

    private bool TrySummonAnyAvailableDropship(
        EntityUid user,
        Entity<DropshipDestinationComponent> destination)
    {
        var dropships = EntityQueryEnumerator<DropshipComponent, TransformComponent>();
        while (dropships.MoveNext(out var uid, out var dropship, out var xform))
        {
            if (HasComp<ThunderdomeMapComponent>(xform.MapUid))
                continue;

            if (!TryGetDropshipComputer(uid, out var computer) ||
                !CanUseDestinationForRemoteOrAutopilot(
                    computer,
                    (uid, dropship),
                    destination,
                    DropshipDestinationKind.LandingZone,
                    true,
                    out _))
            {
                continue;
            }

            if (FlyTo(computer, destination.Owner, user, source: DropshipLaunchSource.PlanetsideTerminal))
                return true;
        }

        return false;
    }

    private bool TryGetAutopilotDropship(
        NetEntity computerNet,
        out Entity<DropshipNavigationComputerComponent> computer,
        out Entity<DropshipComponent> dropship)
    {
        computer = default;
        dropship = default;

        if (!TryGetEntity(computerNet, out var computerId) ||
            !TryComp(computerId, out DropshipNavigationComputerComponent? computerComp) ||
            !computerComp.Hijackable)
        {
            return false;
        }

        computer = (computerId.Value, computerComp);
        return TryGetGridDropship(computer, out dropship);
    }

    private bool TryGetDropshipComputer(EntityUid dropship, out Entity<DropshipNavigationComputerComponent> computer)
    {
        var children = Transform(dropship).ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (!TryComp(child, out DropshipNavigationComputerComponent? computerComp) ||
                !computerComp.Hijackable)
            {
                continue;
            }

            computer = (child, computerComp);
            return true;
        }

        computer = default;
        return false;
    }

    private DropshipDestinationKind ResolveDestinationKind(Entity<DropshipDestinationComponent> destination)
    {
        if (destination.Comp.Kind != DropshipDestinationKind.Auto)
            return destination.Comp.Kind;

        if (destination.Comp.Spawn != null)
            return DropshipDestinationKind.Hangar;

        var xform = Transform(destination);
        if (HasComp<RMCPlanetComponent>(xform.GridUid) ||
            HasComp<RMCPlanetComponent>(xform.MapUid))
        {
            return DropshipDestinationKind.LandingZone;
        }

        if (HasComp<AlmayerComponent>(xform.GridUid) ||
            HasComp<AlmayerComponent>(xform.MapUid))
        {
            return DropshipDestinationKind.Hangar;
        }

        return DropshipDestinationKind.Auto;
    }

    private bool TryResolveRouteHangar(
        Entity<DropshipComponent> dropship,
        DropshipAutopilotComponent autopilot,
        out EntityUid routeHangar)
    {
        if (TryResolveDestinationKind(autopilot.RouteHangar, DropshipDestinationKind.Hangar, out routeHangar))
            return true;

        if (TryResolveDestinationKind(dropship.Comp.Destination, DropshipDestinationKind.Hangar, out routeHangar))
        {
            autopilot.RouteHangar = routeHangar;
            Dirty(dropship.Owner, autopilot);
            return true;
        }

        var query = EntityQueryEnumerator<DropshipDestinationComponent>();
        while (query.MoveNext(out var uid, out var destination))
        {
            if (destination.Ship != dropship.Owner ||
                ResolveDestinationKind((uid, destination)) != DropshipDestinationKind.Hangar)
            {
                continue;
            }

            routeHangar = uid;
            autopilot.RouteHangar = routeHangar;
            Dirty(dropship.Owner, autopilot);
            return true;
        }

        routeHangar = default;
        return false;
    }

    private bool TryResolveLandingZone(DropshipAutopilotComponent autopilot, out EntityUid landingZone)
    {
        return TryResolveDestinationKind(autopilot.LandingZone, DropshipDestinationKind.LandingZone, out landingZone);
    }

    private bool TryResolveDestinationKind(EntityUid? destination, DropshipDestinationKind kind, out EntityUid resolved)
    {
        if (destination is { } uid &&
            !TerminatingOrDeleted(uid) &&
            TryComp(uid, out DropshipDestinationComponent? destinationComp) &&
            ResolveDestinationKind((uid, destinationComp)) == kind)
        {
            resolved = uid;
            return true;
        }

        resolved = default;
        return false;
    }

    private TimeSpan ClampAutopilotDelay(TimeSpan delay)
    {
        var min = Math.Min(_autopilotMinDelay.TotalSeconds, _autopilotMaxDelay.TotalSeconds);
        var max = Math.Max(_autopilotMinDelay.TotalSeconds, _autopilotMaxDelay.TotalSeconds);
        var seconds = Math.Clamp(delay.TotalSeconds, min, max);
        return TimeSpan.FromSeconds(seconds);
    }

    private bool SetAutopilotStatus(
        EntityUid dropship,
        DropshipAutopilotComponent autopilot,
        DropshipAutopilotStatus status,
        string details)
    {
        if (autopilot.Status == status && autopilot.StatusDetails == details)
            return false;

        autopilot.Status = status;
        autopilot.StatusDetails = details;
        Dirty(dropship, autopilot);
        return true;
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

    private bool TryLaunchAutopilotNow(
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
            return false;
        }

        if (FlyTo(computer, target, null, source: DropshipLaunchSource.Autopilot))
        {
            Dirty(dropship.Owner, autopilot);
            SetAutopilotStatus(dropship, autopilot, DropshipAutopilotStatus.InFlight,
                Loc.GetString("rmc-dropship-autopilot-status-detail-launching", ("destination", Name(target))));
            return true;
        }

        autopilot.RetryAt = _timing.CurTime + _autopilotRetryDelay;
        Dirty(dropship.Owner, autopilot);
        SetAutopilotStatus(dropship, autopilot, DropshipAutopilotStatus.Blocked,
            Loc.GetString("rmc-dropship-autopilot-status-detail-busy"));
        return false;
    }

    private bool CanAutopilotLaunchTo(
        Entity<DropshipNavigationComputerComponent> computer,
        Entity<DropshipComponent> dropship,
        EntityUid target,
        out string reason)
    {
        if (!TryComp(target, out DropshipDestinationComponent? destination))
        {
            reason = Loc.GetString("rmc-dropship-autopilot-status-detail-target-invalid");
            return false;
        }

        var kind = ResolveDestinationKind((target, destination));
        if (kind is not (DropshipDestinationKind.Hangar or DropshipDestinationKind.LandingZone))
        {
            reason = Loc.GetString("rmc-dropship-autopilot-status-detail-target-invalid");
            return false;
        }

        return CanUseDestinationForRemoteOrAutopilot(
            computer,
            dropship,
            (target, destination),
            kind,
            true,
            out reason);
    }

    private bool CanRemoteLaunchTo(
        Entity<DropshipNavigationComputerComponent> computer,
        Entity<DropshipComponent> dropship,
        Entity<DropshipDestinationComponent> target,
        out string reason)
    {
        var kind = ResolveDestinationKind(target);
        if (kind is not (DropshipDestinationKind.Hangar or DropshipDestinationKind.LandingZone))
        {
            reason = Loc.GetString("rmc-dropship-remote-error-invalid-destination");
            return false;
        }

        return CanUseDestinationForRemoteOrAutopilot(
            computer,
            dropship,
            target,
            kind,
            true,
            out reason);
    }

    private bool CanUseDestinationForRemoteOrAutopilot(
        Entity<DropshipNavigationComputerComponent> computer,
        Entity<DropshipComponent> dropship,
        Entity<DropshipDestinationComponent> destination,
        DropshipDestinationKind requiredKind,
        bool requireLaunchReady,
        out string reason)
    {
        var kind = ResolveDestinationKind(destination);
        if (requiredKind != DropshipDestinationKind.Auto && kind != requiredKind)
        {
            reason = Loc.GetString("rmc-dropship-remote-error-invalid-destination");
            return false;
        }

        if (kind is not (DropshipDestinationKind.Hangar or DropshipDestinationKind.LandingZone))
        {
            reason = Loc.GetString("rmc-dropship-remote-error-invalid-destination");
            return false;
        }

        if (Transform(computer.Owner).GridUid != dropship.Owner)
        {
            reason = Loc.GetString("rmc-dropship-remote-error-invalid-destination");
            return false;
        }

        if (!IsDestinationAllowedForDropshipComputer(computer, dropship, destination, out reason))
            return false;

        if (!requireLaunchReady)
        {
            reason = string.Empty;
            return true;
        }

        return CanLaunchToDestinationNow(dropship, destination, out reason);
    }

    private bool IsDestinationAllowedForDropshipComputer(
        Entity<DropshipNavigationComputerComponent> computer,
        Entity<DropshipComponent> dropship,
        Entity<DropshipDestinationComponent> destination,
        out string reason)
    {
        // Keep the policy merge point in one place: ert-system restricted docks/tags/bounds
        // should be wired here, while autopilot remains only a route scheduler.
        reason = string.Empty;
        return true;
    }

    private bool CanLaunchToDestinationNow(
        Entity<DropshipComponent> dropship,
        Entity<DropshipDestinationComponent> destination,
        out string reason)
    {
        if (dropship.Comp.Crashed)
        {
            reason = Loc.GetString("rmc-dropship-autopilot-status-detail-crashed");
            return false;
        }

        if (HasComp<FTLComponent>(dropship.Owner))
        {
            reason = Loc.GetString("rmc-dropship-autopilot-status-detail-ftl");
            return false;
        }

        if (IsDropshipPreFlightFueling(out var remaining))
        {
            reason = Loc.GetString("rmc-dropship-autopilot-status-detail-preflight",
                ("minutes", Math.Max(1, (int)remaining.TotalMinutes)));
            return false;
        }

        if (dropship.Comp.Destination == destination.Owner)
        {
            reason = Loc.GetString("rmc-dropship-autopilot-status-detail-target-same");
            return false;
        }

        if (IsDestinationOccupiedByOther(dropship, destination.Comp))
        {
            reason = Loc.GetString("rmc-dropship-autopilot-status-detail-target-occupied");
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool IsDestinationOccupiedByOther(Entity<DropshipComponent> dropship, DropshipDestinationComponent destination)
    {
        return destination.Ship is { } occupiedBy && occupiedBy != dropship.Owner;
    }

    private bool UpdateAutopilot(EntityUid uid, DropshipComponent dropship, DropshipAutopilotComponent autopilot, TimeSpan time)
    {
        if (autopilot.Mode == DropshipAutopilotMode.Disabled)
            return false;

        if (dropship.Crashed)
        {
            SetAutopilotDisabled((uid, dropship), autopilot, null, Loc.GetString("rmc-dropship-autopilot-status-detail-crashed-disabled"));
            return true;
        }

        if (HasComp<FTLComponent>(uid))
        {
            return SetAutopilotStatus(uid, autopilot, DropshipAutopilotStatus.InFlight,
                Loc.GetString("rmc-dropship-autopilot-status-detail-waiting-ftl"));
        }

        if (IsDropshipPreFlightFueling(out var remaining))
        {
            autopilot.NextDepartureAt = null;
            autopilot.RetryAt = time + _autopilotRetryDelay;
            Dirty(uid, autopilot);
            return SetAutopilotStatus(uid, autopilot, DropshipAutopilotStatus.Waiting,
                Loc.GetString("rmc-dropship-autopilot-status-detail-preflight",
                    ("minutes", Math.Max(1, (int)remaining.TotalMinutes))));
        }

        if (autopilot.RetryAt is { } retryAt && retryAt > time)
            return false;

        if (!TryGetNextAutopilotTarget((uid, dropship), autopilot, out var target, out var reason))
        {
            autopilot.NextDepartureAt = null;
            autopilot.RetryAt = time + _autopilotRetryDelay;
            Dirty(uid, autopilot);
            var holdingAtRouteHangar = Loc.GetString("rmc-dropship-autopilot-status-detail-holding-hangar");
            return SetAutopilotStatus(uid, autopilot,
                reason == holdingAtRouteHangar ? DropshipAutopilotStatus.Ready : DropshipAutopilotStatus.Error,
                reason);
        }

        if (!TryGetDropshipComputer(uid, out var computer))
        {
            autopilot.NextDepartureAt = null;
            autopilot.RetryAt = time + _autopilotRetryDelay;
            Dirty(uid, autopilot);
            return SetAutopilotStatus(uid, autopilot, DropshipAutopilotStatus.Error,
                Loc.GetString("rmc-dropship-autopilot-status-detail-no-computer"));
        }

        if (!CanAutopilotLaunchTo(computer, (uid, dropship), target, out reason))
        {
            autopilot.NextDepartureAt = null;
            autopilot.RetryAt = time + _autopilotRetryDelay;
            Dirty(uid, autopilot);
            return SetAutopilotStatus(uid, autopilot, DropshipAutopilotStatus.Blocked, reason);
        }

        if (autopilot.NextDepartureAt == null)
        {
            autopilot.NextDepartureAt = time + ClampAutopilotDelay(autopilot.Delay);
            autopilot.RetryAt = null;
            Dirty(uid, autopilot);
            return SetAutopilotStatus(uid, autopilot, DropshipAutopilotStatus.Waiting,
                Loc.GetString("rmc-dropship-autopilot-status-detail-waiting-launch", ("destination", Name(target))));
        }

        if (autopilot.NextDepartureAt > time)
            return false;

        return TryLaunchAutopilotNow((uid, dropship), autopilot, computer, target);
    }

    private void OnDepartureLocationFTLStarted(Entity<DropshipDestinationComponent> ent, ref DropshipRelayedEvent<FTLStartedEvent> args)
    {
        ToggleLandingLights(ent, false);
    }

    private void OnDestinationLocationFTLCompleted(Entity<DropshipDestinationComponent> ent, ref DropshipRelayedEvent<FTLCompletedEvent> args)
    {
        if (ent.Comp.Ship != args.Relayer)
            return;

        QueueDel(ent.Comp.ArrivalSoundEntity);
        ent.Comp.ArrivalSoundEntity = null;
        Dirty(ent);

        ToggleLandingLights(ent, false);
    }

    private void OnDestinationLocationFTLUpdated(Entity<DropshipDestinationComponent> ent, ref DropshipRelayedEvent<FTLUpdatedEvent> args)
    {
        if (ent.Comp.Ship != args.Relayer)
            return;

        if (!TryComp(ent.Comp.Ship, out FTLComponent? ftl))
            return;

        if (ftl.State is not FTLState.Arriving)
            return;

        if (TryComp<DropshipComponent>(ent.Comp.Ship, out var dropship) &&
            ftl.State == FTLState.Arriving &&
            dropship.Destination is { } destination)
        {
            var audio = Audio.PlayPvs(dropship.ArrivalSound, destination);
            if (audio != null)
            {
                ent.Comp.ArrivalSoundEntity = audio.Value.Entity;
                Dirty(ent);
            }
        }

        ToggleLandingLights(ent, true);
    }

    private void UnlockAllDoors(Entity<DropshipNavigationComputerComponent> ent)
    {
        if (_transform.GetGrid(ent.Owner) is not { } grid ||
            !TryComp(grid, out DropshipComponent? dropship) ||
            dropship.Crashed)
        {
            return;
        }

        if (TryComp(grid, out FTLComponent? ftl) &&
            ftl.State is FTLState.Travelling or FTLState.Arriving)
        {
            return;
        }

        var enumerator = Transform(grid).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            if (!_dockingQuery.HasComp(child) ||
                !_doorBoltQuery.HasComp(child))
                continue;

            UnlockDoor(child);
        }
    }

    public override bool FlyTo(
        Entity<DropshipNavigationComputerComponent> computer,
        EntityUid destination,
        EntityUid? user,
        bool hijack = false,
        float? startupTime = null,
        float? hyperspaceTime = null,
        bool offset = false,
        DropshipLaunchSource source = DropshipLaunchSource.ManualNavigation)
    {
        base.FlyTo(computer, destination, user, hijack, startupTime, hyperspaceTime, offset, source);

        _hijack = hijack;
        var dropshipId = Transform(computer).GridUid;
        _dropshipId = dropshipId ?? EntityUid.Invalid;
        if (!TryComp(dropshipId, out ShuttleComponent? shuttleComp))
        {
            Log.Warning($"Tried to launch {ToPrettyString(computer)} outside of a shuttle.");
            return false;
        }

        if (HasComp<FTLComponent>(dropshipId))
        {
            Log.Warning($"Tried to launch shuttle {ToPrettyString(dropshipId)} in FTL");
            return false;
        }

        var dropship = EnsureComp<DropshipComponent>(dropshipId.Value);
        if (dropship.Crashed)
        {
            Log.Warning($"Tried to launch crashed dropship {ToPrettyString(dropshipId.Value)}");
            return false;
        }

        var newDestination = CompOrNull<DropshipDestinationComponent>(destination);
        if (dropship.Destination == destination)
        {
            if (user != null && !_skills.HasSkill(user.Value, computer.Comp.Skill, computer.Comp.FlyBySkillLevel))
            {
                var msg = Loc.GetString("rmc-dropship-flyby-no-skill");
                _popup.PopupEntity(msg, user.Value, user.Value, PopupType.MediumCaution);
                return false;
            }

            EnsureComp<DropshipInFlyByComponent>(dropshipId.Value);
        }
        else if (!hijack &&
                 newDestination != null &&
                 newDestination.Ship != null &&
                 newDestination.Ship != dropshipId.Value)
        {
            Log.Warning($"{ToPrettyString(user)} tried to launch to occupied dropship destination {ToPrettyString(destination)}");
            return false;
        }

        var autopilot = EnsureComp<DropshipAutopilotComponent>(dropshipId.Value);
        if (source == DropshipLaunchSource.RoundInit &&
            newDestination != null &&
            ResolveDestinationKind((destination, newDestination)) == DropshipDestinationKind.Hangar)
        {
            autopilot.RouteHangar ??= destination;
            autopilot.Delay = ClampAutopilotDelay(autopilot.Delay == TimeSpan.Zero ? _autopilotDefaultDelay : autopilot.Delay);
            Dirty(dropshipId.Value, autopilot);
        }

        DisableAutopilotForManualOverride((dropshipId.Value, dropship), autopilot, source, user);

        if (TryComp(dropship.Destination, out DropshipDestinationComponent? oldDestination))
        {
            oldDestination.Ship = null;
            Dirty(dropship.Destination.Value, oldDestination);
        }

        if (newDestination != null)
        {
            newDestination.Ship = dropshipId;
            Dirty(destination, newDestination);
        }

        if (hyperspaceTime == null)
        {
            if (hijack)
            {
                hyperspaceTime = (float) _hijackTravelTime.TotalSeconds;
            }
            else
            {
                var hasSkill = user != null && _skills.HasSkill(user.Value, computer.Comp.Skill, computer.Comp.MultiplierSkillLevel);
                var rechargeMultiplier = hasSkill ? computer.Comp.SkillRechargeMultiplier : 1f;
                var flyBy = dropship.Destination == destination;
                if (flyBy)
                {
                    hyperspaceTime = (float) _flyByTime.TotalSeconds;
                    if (hasSkill)
                        hyperspaceTime *= computer.Comp.SkillFlyByMultiplier;
                }
                else
                {
                    hyperspaceTime = _shuttle.DefaultTravelTime;
                    if (hasSkill)
                        hyperspaceTime *= computer.Comp.SkillTravelMultiplier;
                }

                dropship.RechargeTime = TimeSpan.FromSeconds(_config.GetCVar(CCVars.FTLCooldown) * rechargeMultiplier);

                foreach (var point in dropship.AttachmentPoints)
                {
                    if (TryComp(point, out DropshipEnginePointComponent? engine) &&
                        _container.TryGetContainer(point, engine.ContainerId, out var container))
                    {
                        foreach (var contained in container.ContainedEntities)
                        {
                            if (TryComp(contained, out DropshipFlightMultiplierComponent? flightMult))
                            {
                                if (flyBy)
                                    hyperspaceTime /= flightMult.Multiplier;
                                else
                                    hyperspaceTime *= flightMult.Multiplier;
                            }

                            if (TryComp(contained, out DropshipRechargeMultiplierComponent? rechargeMult))
                                dropship.RechargeTime *= rechargeMult.Multiplier;
                        }
                    }
                }

                hyperspaceTime += _config.GetCVar(CCVars.FTLArrivalTime);
            }
        }

        dropship.Destination = destination;
        Dirty(dropshipId.Value, dropship);

        var destTransform = Transform(destination);
        var destCoords = _transform.GetMoverCoordinates(destination, destTransform);
        var rotation = destTransform.LocalRotation;

        if (TryComp(dropshipId, out PhysicsComponent? physics))
        {
            _physics.SetLocalCenter(dropshipId.Value, physics, Vector2.Zero);
            destCoords = destCoords.Offset(-physics.LocalCenter);
        }

        if (offset)
            destCoords = destCoords.Offset(new Vector2(-0.5f, -0.5f));

        _shuttle.FTLToCoordinates(dropshipId.Value, shuttleComp, destCoords, rotation, startupTime: startupTime, hyperspaceTime: hyperspaceTime);

        if (hijack)
        {
            if (user != null)
            {
                var xenoText = Loc.GetString("rmc-announcement-dropship-hijack-hive");
                _xenoAnnounce.AnnounceSameHive(user.Value, xenoText);
                Audio.PlayPvs(dropship.LocalHijackSound, dropshipId.Value);

                var marineText = Loc.GetString("rmc-announcement-dropship-hijack");
                _marineAnnounce.AnnounceARESStaging(dropshipId.Value, marineText, dropship.MarineHijackSound, new LocId("rmc-announcement-dropship-message"));

                var generalQuartersText = Loc.GetString("rmc-announcement-general-quarters");
                Timer.Spawn(TimeSpan.FromSeconds(10), () =>
                {
                    _alertLevelSystem.Set(RMCAlertLevels.Red, dropshipId.Value, false, false);
                    _marineAnnounce.AnnounceARESStaging(dropshipId.Value, generalQuartersText, dropship.GeneralQuartersSound, null);
                });
            }

            // Add 10 seconds to compensate for the arriving times
            dropship.HijackLandAt = _timing.CurTime + TimeSpan.FromSeconds(hyperspaceTime.Value) + TimeSpan.FromSeconds(10);
            Dirty(dropshipId.Value, dropship);
        }

        _adminLog.Add(LogType.RMCDropshipLaunch,
            $"{ToPrettyString(user):player} {(hijack ? "hijacked" : "launched")} {ToPrettyString(dropshipId):dropship} to {ToPrettyString(destination):destination}");

        RefreshUI(computer);
        RefreshRemoteControlConsoles();
        return true;
    }

    protected override void RefreshUI(Entity<DropshipNavigationComputerComponent> computer)
    {
        if (!_ui.IsUiOpen(computer.Owner, DropshipNavigationUiKey.Key))
            return;

        if (Transform(computer).GridUid is not { } grid)
            return;

        var doorLockStatus = GetDoorLockStatus(grid);
        var autopilotStatus = GetNavigationAutopilotStatus(grid);

        if (!TryComp(grid, out FTLComponent? ftl) ||
            !ftl.Running ||
            ftl.State == FTLState.Available)
        {
            NetEntity? flyBy = null;
            var destinations = new List<Destination>();
            var query = EntityQueryEnumerator<DropshipDestinationComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                var netDestination = GetNetEntity(uid);
                if (comp.Ship == grid)
                {
                    flyBy = netDestination;
                    continue;
                }

                var destination = new Destination(
                    netDestination,
                    Name(uid),
                    comp.Ship != null,
                    HasComp<PrimaryLandingZoneComponent>(uid)
                );
                destinations.Add(destination);
            }

            var state = new DropshipNavigationDestinationsBuiState(flyBy, destinations, doorLockStatus, computer.Comp.LaunchAlarmStatus, autopilotStatus);
            _ui.SetUiState(computer.Owner, DropshipNavigationUiKey.Key, state);
            return;
        }

        var destinationName = string.Empty;
        var departureName = string.Empty;
        if (TryComp(grid, out DropshipComponent? dropship))
        {
            if (dropship.Destination is { } destinationUid)
                destinationName = Name(destinationUid);
            else
            {
                Log.Error($"Found in-travel dropship {ToPrettyString(grid)} with invalid destination");
            }

            if (dropship.DepartureLocation is { } departureUid)
            {
                departureName = Name(departureUid);
            }
        }

        var travelState = new DropshipNavigationTravellingBuiState(ftl.State, ftl.StateTime, destinationName, departureName, doorLockStatus, computer.Comp.LaunchAlarmStatus, autopilotStatus);
        _ui.SetUiState(computer.Owner, DropshipNavigationUiKey.Key, travelState);
    }

    private DropshipNavigationAutopilotStatus? GetNavigationAutopilotStatus(EntityUid dropship)
    {
        if (!TryComp(dropship, out DropshipAutopilotComponent? autopilot) ||
            autopilot.Mode == DropshipAutopilotMode.Disabled)
        {
            return null;
        }

        return new DropshipNavigationAutopilotStatus(
            autopilot.Mode,
            autopilot.Status,
            autopilot.StatusDetails,
            GetAutopilotDepartInSeconds(autopilot));
    }

    private int? GetAutopilotDepartInSeconds(DropshipAutopilotComponent autopilot)
    {
        if (autopilot.NextDepartureAt is not { } departAt)
            return null;

        return Math.Max(0, (int)Math.Ceiling((departAt - _timing.CurTime).TotalSeconds));
    }

    private void RefreshRemoteControlConsoles()
    {
        var consoles = EntityQueryEnumerator<DropshipRemoteControlConsoleComponent>();
        while (consoles.MoveNext(out var uid, out var console))
        {
            RefreshRemoteControlConsole((uid, console));
        }
    }

    private void RefreshRemoteControlConsole(Entity<DropshipRemoteControlConsoleComponent> console)
    {
        if (!_ui.IsUiOpen(console.Owner, DropshipRemoteControlUiKey.Key))
            return;

        var controllableDropships = new List<(EntityUid Uid, DropshipComponent Dropship, Entity<DropshipNavigationComputerComponent> Computer)>();
        var dropships = new List<DropshipRemoteControlDropshipEntry>();
        var dropshipQuery = EntityQueryEnumerator<DropshipComponent>();
        while (dropshipQuery.MoveNext(out var uid, out var dropship))
        {
            if (!TryGetDropshipComputer(uid, out var computer))
                continue;

            controllableDropships.Add((uid, dropship, computer));
            TryComp(uid, out DropshipAutopilotComponent? autopilot);

            var location = dropship.Destination is { } destination && Exists(destination)
                ? Name(destination)
                : Loc.GetString("rmc-dropship-remote-location-unknown");

            if (TryComp(uid, out FTLComponent? ftl) &&
                ftl.State is FTLState.Starting or FTLState.Travelling or FTLState.Arriving)
            {
                location = Loc.GetString("rmc-dropship-remote-location-in-flight", ("location", location));
            }
            else if (ftl != null && ftl.State == FTLState.Cooldown)
            {
                location = Loc.GetString("rmc-dropship-remote-location-refueling", ("location", location));
            }

            NetEntity? routeHangar = null;
            var routeHangarName = Loc.GetString("rmc-dropship-remote-route-not-set");
            if (autopilot != null && TryResolveRouteHangar((uid, dropship), autopilot, out var routeHangarUid))
            {
                routeHangar = GetNetEntity(routeHangarUid);
                routeHangarName = Name(routeHangarUid);
            }
            else if (TryResolveDestinationKind(dropship.Destination, DropshipDestinationKind.Hangar, out routeHangarUid))
            {
                routeHangar = GetNetEntity(routeHangarUid);
                routeHangarName = Name(routeHangarUid);
            }

            var mode = autopilot?.Mode ?? DropshipAutopilotMode.Disabled;
            var delay = autopilot?.Delay ?? _autopilotDefaultDelay;
            var lz = autopilot?.LandingZone is { } selectedLz && Exists(selectedLz)
                ? GetNetEntity(selectedLz)
                : (NetEntity?)null;

            dropships.Add(new DropshipRemoteControlDropshipEntry(
                GetNetEntity(computer.Owner),
                GetNetEntity(uid),
                Name(uid),
                location,
                routeHangar,
                routeHangarName,
                lz,
                mode,
                (int)Math.Round(delay.TotalSeconds),
                autopilot == null ? null : GetAutopilotDepartInSeconds(autopilot),
                autopilot?.Status ?? DropshipAutopilotStatus.Offline,
                autopilot?.StatusDetails ?? string.Empty,
                HasComp<FTLComponent>(uid),
                dropship.Crashed));
        }

        var destinations = new List<DropshipRemoteControlDestinationEntry>();
        var hangars = new List<DropshipRemoteControlDestinationEntry>();
        var landingZones = new List<DropshipRemoteControlDestinationEntry>();
        var destinationQuery = EntityQueryEnumerator<DropshipDestinationComponent>();
        while (destinationQuery.MoveNext(out var uid, out var destination))
        {
            var kind = ResolveDestinationKind((uid, destination));
            if (kind is not (DropshipDestinationKind.Hangar or DropshipDestinationKind.LandingZone))
                continue;

            var availableDropships = new List<NetEntity>();
            foreach (var candidate in controllableDropships)
            {
                if (CanUseDestinationForRemoteOrAutopilot(
                        candidate.Computer,
                        (candidate.Uid, candidate.Dropship),
                        (uid, destination),
                        kind,
                        false,
                        out _))
                {
                    availableDropships.Add(GetNetEntity(candidate.Uid));
                }
            }

            if (availableDropships.Count == 0)
                continue;

            var entry = new DropshipRemoteControlDestinationEntry(
                GetNetEntity(uid),
                Name(uid),
                destination.Ship is { } ship ? GetNetEntity(ship) : null,
                HasComp<PrimaryLandingZoneComponent>(uid),
                availableDropships);

            destinations.Add(entry);
            if (kind == DropshipDestinationKind.Hangar)
                hangars.Add(entry);
            else
                landingZones.Add(entry);
        }

        NetEntity? linkedLandingZone = null;
        var linkedLandingZoneName = Loc.GetString("rmc-dropship-remote-linked-lz-none");
        if (TryResolveRemoteLinkedLandingZone(console, out var linked))
        {
            linkedLandingZone = GetNetEntity(linked.Owner);
            linkedLandingZoneName = Name(linked.Owner);
        }

        var state = new DropshipRemoteControlBuiState(
            console.Comp.Kind,
            linkedLandingZone,
            linkedLandingZoneName,
            dropships,
            destinations,
            hangars,
            landingZones,
            (int)Math.Round(_autopilotDefaultDelay.TotalSeconds),
            (int)Math.Round(_autopilotMinDelay.TotalSeconds),
            (int)Math.Round(_autopilotMaxDelay.TotalSeconds));

        _ui.SetUiState(console.Owner, DropshipRemoteControlUiKey.Key, state);
    }

    protected override bool IsShuttle(EntityUid dropship)
    {
        return HasComp<ShuttleComponent>(dropship);
    }

    protected override bool IsInFTL(EntityUid dropship)
    {
        return HasComp<FTLComponent>(dropship);
    }

    protected override void RefreshUI()
    {
        var computers = EntityQueryEnumerator<DropshipNavigationComputerComponent>();
        while (computers.MoveNext(out var uid, out var comp))
        {
            RefreshUI((uid, comp));
        }
    }

    private void SetDocks(EntityUid dropship, DoorLocation location)
    {
        var shouldLock = false;
        var doors = new HashSet<Entity<DoorBoltComponent>>();

        // Lock all doors if at least one is unlocked.
        var enumerator = Transform(dropship).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            if (!_dockingQuery.HasComp(child))
                continue;

            if (!_doorBoltQuery.TryComp(child, out var bolt))
                continue;

            doors.Add((child, bolt));

            if (bolt.BoltsDown)
                continue;

            shouldLock = true;
        }

        foreach (var door in doors)
        {
            if (location != DoorLocation.None)
            {
                // Only lock/unlock doors with the same location as the pressed button.
                if (!_doorQuery.TryComp(door, out var doorComp) || doorComp.Location != location)
                    continue;

                shouldLock = !door.Comp.BoltsDown;
            }

            if (shouldLock)
                LockDoor(door.Owner);
            else
                UnlockDoor(door.Owner);
        }
    }

    private Dictionary<DoorLocation, bool> GetDoorLockStatus(EntityUid dropship)
    {
        var doorLockStatus = new Dictionary<DoorLocation, bool>();
        var enumerator = Transform(dropship).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            if (_dockingQuery.HasComp(child) &&
                _doorBoltQuery.TryComp(child, out var bolt) &&
                _doorQuery.TryComp(child, out var door))
            {
                doorLockStatus.TryAdd(door.Location, bolt.BoltsDown);
            }
        }

        return doorLockStatus;
    }

    /// <summary>
    ///     Relays events to equipment slotted in the dropship's weapon, utility and electronic hardpoints.
    /// </summary>
    /// <param name="ent">The dropship entity that received the event that will be relayed</param>
    /// <param name="args">The raised event that is forwarded</param>
    /// <typeparam name="TEvent">The type of the event</typeparam>
    private void RelayToMountedEntities<TEvent>(Entity<DropshipComponent> ent, TEvent args) where TEvent : struct
    {
        foreach (var attachPoint in ent.Comp.AttachmentPoints)
        {
            BaseContainer? container = null;
            if (TryComp(attachPoint, out DropshipWeaponPointComponent? weaponPoint))
                _container.TryGetContainer(attachPoint, weaponPoint.WeaponContainerSlotId, out container);
            else if (TryComp(attachPoint, out DropshipUtilityPointComponent? utilityPoint))
                _container.TryGetContainer(attachPoint, utilityPoint.UtilitySlotId, out container);
            else if (TryComp(attachPoint, out DropshipElectronicSystemPointComponent? electronicPoint))
                _container.TryGetContainer(attachPoint, electronicPoint.ContainerId, out container);

            if (container == null)
                continue;

            foreach (var mountedEntity in container.ContainedEntities)
            {
                var relayedEvent = new DropshipRelayedEvent<TEvent>(args, attachPoint);
                RaiseLocalEvent(mountedEntity, ref relayedEvent);
            }
        }
    }

    /// <summary>
    ///     Relays events to the dropship's destination.
    /// </summary>
    /// <param name="ent">The dropship entity that received the event that will be relayed</param>
    /// <param name="args">The raised event that is forwarded</param>
    /// <typeparam name="TEvent">The type of the event</typeparam>
    private void RelayToDropshipDestination<TEvent>(Entity<DropshipComponent> ent, TEvent args) where TEvent : struct
    {
        if (ent.Comp.Destination is not { } destination)
            return;

        var relayedEvent = new DropshipRelayedEvent<TEvent>(args, ent);
        RaiseLocalEvent(destination, ref relayedEvent);
    }

    /// <summary>
    ///     Relays events to the dropship's departure location.
    /// </summary>
    /// <param name="ent">The dropship entity that received the event that will be relayed</param>
    /// <param name="args">The raised event that is forwarded</param>
    /// <typeparam name="TEvent">The type of the event</typeparam>
    private void RelayToDropshipDepartureLocation<TEvent>(Entity<DropshipComponent> ent, TEvent args) where TEvent : struct
    {
        if (ent.Comp.DepartureLocation is not { } departureLocation)
            return;

        var relayedEvent = new DropshipRelayedEvent<TEvent>(args, ent);
        RaiseLocalEvent(departureLocation, ref relayedEvent);
    }

    private void ToggleLandingLights(EntityUid destination, bool enable, DropshipDestinationComponent? destinationComponent = null)
    {
        if (!Resolve(destination, ref destinationComponent, false))
            return;

        var time = _timing.CurTime;
        var lights = _entityLookup.GetEntitiesInRange<LandingLightComponent>(destination.ToCoordinates(), destinationComponent.LightSearchRadius);
        foreach (var light in lights)
        {
            if (!TryComp<LandingLightComponent>(light, out var lightComp))
                continue;

            lightComp.Enabled = enable;
            if (enable)
                lightComp.StartTime = time;

            Dirty(light, lightComp);

            _appearance.SetData(light, LandingLightVisuals.Off, !enable);
            _appearance.SetData(light, LandingLightVisuals.On, enable);

            _pointLight.SetEnabled(light, enable);
        }
    }

    public void LockDoor(Entity<DoorBoltComponent?> door)
    {
        if (_doorQuery.TryComp(door, out var doorComp) &&
            doorComp.State != DoorState.Closed)
        {
            var oldCheck = doorComp.PerformCollisionCheck;
            doorComp.PerformCollisionCheck = false;

            _door.StartClosing(door);
            _door.OnPartialClose(door);

            doorComp.PerformCollisionCheck = oldCheck;
        }

        if (_doorBoltQuery.Resolve(door, ref door.Comp, false))
            _door.SetBoltsDown((door.Owner, door.Comp), true);
    }

    public void UnlockDoor(Entity<DoorBoltComponent?> door)
    {
        if (_doorBoltQuery.Resolve(door, ref door.Comp, false))
            _door.SetBoltsDown((door.Owner, door.Comp), false);
    }

    public void RaiseUpdate(EntityUid shuttle)
    {
        var ev = new FTLUpdatedEvent();
        RaiseLocalEvent(shuttle, ref ev);

        if (!TryComp(shuttle, out DropshipComponent? dropship))
            return;

        RelayToDropshipDestination((shuttle, dropship), ev);
    }

    public bool AnyHijacked()
    {
        var dropships = EntityQueryEnumerator<DropshipComponent>();
        while (dropships.MoveNext(out var dropship))
        {
            if (dropship.Crashed)
                return true;
        }

        return false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;

        var dropships = EntityQueryEnumerator<DropshipComponent, FTLComponent>();
        while (dropships.MoveNext(out var uid, out var dropship, out var ftl))
        {
            if (!dropship.Crashed)
                continue;

            ftl.VisualizerProto = null;

            if (dropship.Destination == null)
                continue;

            var destinationCoords = _transform.GetMapCoordinates(dropship.Destination.Value);
            var destinationEntityCoords = _transform.GetMoverCoordinates(dropship.Destination.Value);
            var destinationFilter = Filter.BroadcastMap(destinationCoords.MapId);

            if (dropship.HijackLandAt - dropship.AnnounceCrashTime <= time && !dropship.AnnouncedCrash)
            {
                dropship.AnnouncedCrash = true;
                Dirty(uid, dropship);

                _marineAnnounce.AnnounceToMarines(Loc.GetString("rmc-announcement-emergency-dropship-crash"), dropship.CrashWarningSound);
                continue;
            }

            if (dropship.HijackLandAt - dropship.PlayIncomingSoundTime <= time && !dropship.DidIncomingSound)
            {
                dropship.DidIncomingSound = true;
                Dirty(uid, dropship);

                Audio.PlayGlobal(dropship.IncomingSound, destinationFilter, true);
                continue;
            }

            if (dropship.HijackLandAt - dropship.ExplodeTime <= time && !dropship.DidExplosion)
            {
                dropship.DidExplosion = true;
                Dirty(uid, dropship);

                Audio.PlayGlobal(dropship.CrashSound, destinationFilter, true);
                _rmcFlammable.SpawnFireDiamond(dropship.FireId, destinationEntityCoords, dropship.FireRange, 11);
                _rmcExplosion.QueueExplosion(destinationCoords, "RMCOB", 50000, 1500, 90, uid);

                continue;
            }
        }

        var autopilotChanged = false;
        var autopilotActive = false;
        var autopilotQuery = EntityQueryEnumerator<DropshipComponent, DropshipAutopilotComponent>();
        while (autopilotQuery.MoveNext(out var uid, out var dropship, out var autopilot))
        {
            if (autopilot.Mode != DropshipAutopilotMode.Disabled)
                autopilotActive = true;

            autopilotChanged |= UpdateAutopilot(uid, dropship, autopilot, time);
        }

        if (autopilotChanged)
        {
            RefreshUI();
            RefreshRemoteControlConsoles();
        }
        else if (autopilotActive && time >= _nextAutopilotUiRefresh)
        {
            _nextAutopilotUiRefresh = time + TimeSpan.FromSeconds(1);
            RefreshUI();
            RefreshRemoteControlConsoles();
        }

        if (Count<PrimaryLandingZoneComponent>() > 0)
            return;

        if (_gameTicker.RoundDuration() < _lzPrimaryAutoDelay)
            return;

        foreach (var primaryLZCandidate in GetPrimaryLZCandidates())
        {
            if (TryDesignatePrimaryLZ(default, primaryLZCandidate))
                break;
        }
    }
}
