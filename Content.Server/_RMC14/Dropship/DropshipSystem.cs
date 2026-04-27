using System.Linq;
using System.Numerics;
using Content.Server._RMC14.GameStates;
using Content.Server._RMC14.Marines;
using Content.Server._RMC14.Shuttles;
using Content.Server.Doors.Systems;
using Content.Server.GameTicking;
using Content.Server.Shuttles;
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
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Timing;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Dropship;

[RegisterComponent]
public sealed partial class RMCExpectedDockComponent : Component
{
    [ViewVariables]
    public EntityUid Destination;

    [ViewVariables]
    public EntityUid TargetGrid;

    [ViewVariables]
    public EntityUid ShuttleDock;

    [ViewVariables]
    public EntityUid TargetDock;

    [ViewVariables]
    public EntityCoordinates Coordinates;

    [ViewVariables]
    public Angle Angle;

    [ViewVariables]
    public DockingConfig? Config;

    [ViewVariables]
    public RMCShuttleDockingClass DockingClass;

    [ViewVariables]
    public Guid RequestId;

    [ViewVariables]
    public string? Call;

    [ViewVariables]
    public bool Confirmed;

    [ViewVariables]
    public EntityUid? ActualShuttleDock;

    [ViewVariables]
    public EntityUid? ActualTargetDock;

    [ViewVariables]
    public string? FailureReason;
}

[RegisterComponent]
public sealed partial class RMCRestrictedShuttleComponent : Component
{
    [ViewVariables]
    public Guid RequestId;

    [ViewVariables]
    public string? Call;
}

[ByRefEvent]
public readonly record struct RMCDockingVerificationFailedEvent(
    EntityUid Shuttle,
    EntityUid Destination,
    EntityUid TargetGrid,
    EntityUid ShuttleDock,
    EntityUid TargetDock,
    EntityUid? ActualShuttleDock,
    EntityUid? ActualTargetDock,
    Guid RequestId,
    string? Call,
    RMCShuttleDockingClass DockingClass,
    string Reason);

public sealed class DropshipSystem : SharedDropshipSystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DoorSystem _door = default!;
    [Dependency] private readonly DockingSystem _docking = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
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
    private EntityQuery<FixturesComponent> _fixturesQuery;
    private EntityQuery<FTLSmashImmuneComponent> _ftlSmashImmuneQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    private const float RestrictedDockingStagingOffset = 0.1f;
    private const float RestrictedDockingClusterRadius = 5f;
    private const float RestrictedDockingShuttleClusterRadius = 3f;

    private readonly HashSet<EntityUid> _restrictedDockingObstacles = new();

    private TimeSpan _lzPrimaryAutoDelay;
    private TimeSpan _flyByTime;
    private TimeSpan _hijackTravelTime;

    private EntityUid _dropshipId;
    private bool _hijack;

    private const float DepartureLocationSearchRange = 12;

    public bool CanUseDestinationForShuttle(
        Entity<DropshipNavigationComputerComponent> computer,
        EntityUid destination,
        out string reason)
    {
        if (!CanUseDestination(computer, destination, out reason))
            return false;

        if (!computer.Comp.RequiresRestrictedDestination)
            return true;

        if (TryComp(destination, out DropshipDestinationComponent? destinationComp) &&
            Transform(computer).GridUid is { } currentGrid &&
            destinationComp.Ship == currentGrid)
        {
            return true;
        }

        if (!TryComp(destination, out DockingComponent? destinationDock))
            return true;

        return TryGetDockingTravelTarget(
            computer,
            destination,
            destinationDock,
            out _,
            out reason);
    }

    public void SetPlayerRouteLock(EntityUid shuttle, EntityUid? destination)
    {
        var computerQuery = EntityQueryEnumerator<DropshipNavigationComputerComponent, TransformComponent>();
        while (computerQuery.MoveNext(out var uid, out var computer, out var xform))
        {
            if (xform.GridUid != shuttle)
                continue;

            computer.PlayerDestinationLockEnabled = true;
            computer.PlayerAllowedDestination = destination;
            Dirty(uid, computer);
        }

        if (Exists(shuttle))
            RaiseUpdate(shuttle);
    }

    public void ClearPlayerRouteLock(EntityUid shuttle)
    {
        var computerQuery = EntityQueryEnumerator<DropshipNavigationComputerComponent, TransformComponent>();
        while (computerQuery.MoveNext(out var uid, out var computer, out var xform))
        {
            if (xform.GridUid != shuttle)
                continue;

            computer.PlayerDestinationLockEnabled = false;
            computer.PlayerAllowedDestination = null;
            Dirty(uid, computer);
        }

        if (Exists(shuttle))
            RaiseUpdate(shuttle);
    }

    public void SetCurrentDestination(EntityUid shuttle, EntityUid? destination)
    {
        var dropship = EnsureComp<DropshipComponent>(shuttle);
        dropship.Destination = destination;
        dropship.DepartureLocation = destination;
        Dirty(shuttle, dropship);
    }

    public void ClearCurrentDestinationIf(EntityUid shuttle, EntityUid destination)
    {
        if (!TryComp(shuttle, out DropshipComponent? dropship))
            return;

        if (dropship.Destination == destination)
            dropship.Destination = null;

        if (dropship.DepartureLocation == destination)
            dropship.DepartureLocation = null;

        Dirty(shuttle, dropship);
    }

    public override void Initialize()
    {
        base.Initialize();

        _dockingQuery = GetEntityQuery<DockingComponent>();
        _doorQuery = GetEntityQuery<DoorComponent>();
        _doorBoltQuery = GetEntityQuery<DoorBoltComponent>();
        _fixturesQuery = GetEntityQuery<FixturesComponent>();
        _ftlSmashImmuneQuery = GetEntityQuery<FTLSmashImmuneComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<DropshipNavigationComputerComponent, DropshipLockoutDoAfterEvent>(OnNavigationLockout);

        SubscribeLocalEvent<DropshipComponent, FTLRequestEvent>(OnFtlRequested);
        SubscribeLocalEvent<DropshipComponent, FTLStartedEvent>(OnFTLStarted);
        SubscribeLocalEvent<DropshipComponent, FTLCompletedEvent>(OnFTLCompleted);
        SubscribeLocalEvent<DropshipComponent, FTLUpdatedEvent>(OnFTLUpdated);
        SubscribeLocalEvent<DropshipComponent, BeforeFTLStartedEvent>(OnBeforeFTLStarted);

        SubscribeLocalEvent<DockEvent>(OnDocked);
        SubscribeLocalEvent<DockingComponent, DockEvent>(OnDockingPortDocked);
        SubscribeLocalEvent<DropshipDestinationComponent, MapInitEvent>(OnRestrictedDestinationMapInit);
        SubscribeLocalEvent<RMCShuttleMobileDockComponent, MapInitEvent>(OnRestrictedDockMapInit);

        SubscribeLocalEvent<DropshipInFlyByComponent, FTLCompletedEvent>(OnInFlyByFTLCompleted);

        SubscribeLocalEvent<DropshipDestinationComponent, DropshipRelayedEvent<FTLStartedEvent>>(OnDepartureLocationFTLStarted);
        SubscribeLocalEvent<DropshipDestinationComponent, DropshipRelayedEvent<FTLCompletedEvent>>(OnDestinationLocationFTLCompleted);
        SubscribeLocalEvent<DropshipDestinationComponent, DropshipRelayedEvent<FTLUpdatedEvent>>(OnDestinationLocationFTLUpdated);

        Subs.BuiEvents<DropshipNavigationComputerComponent>(DropshipNavigationUiKey.Key,
            subs =>
            {
                subs.Event<DropshipLockdownMsg>(OnDropshipNavigationLockdownMsg);
                subs.Event<DropshipRemoteControlToggleMsg>(OnDropshipRemoteControlToggleMsg);
                subs.Event<DropshipLaunchAlarmToggleMsg>(OnDropshipLaunchAlarmToggleMsg);
            });

        Subs.CVar(_config, RMCCVars.RMCLandingZonePrimaryAutoMinutes, v => _lzPrimaryAutoDelay = TimeSpan.FromMinutes(v), true);
        Subs.CVar(_config, RMCCVars.RMCDropshipFlyByTimeSeconds, v => _flyByTime = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_config, RMCCVars.RMCDropshipHijackTravelTimeSeconds, v => _hijackTravelTime = TimeSpan.FromSeconds(v), true);
    }

    private void OnRestrictedDockMapInit<T>(EntityUid uid, T component, MapInitEvent args) where T : Component
    {
        AnchorRestrictedDock(uid);
    }

    private void OnRestrictedDestinationMapInit(Entity<DropshipDestinationComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.Reserved &&
            ent.Comp.LandingClasses.Count == 0 &&
            ent.Comp.LandingTags.Count == 0)
        {
            return;
        }

        AnchorRestrictedDock(ent);
    }

    private void AnchorRestrictedDock(EntityUid uid)
    {
        if (!TryComp(uid, out TransformComponent? xform) ||
            xform.Anchored)
        {
            return;
        }

        if (_transform.AnchorEntity(uid, xform))
        {
            Log.Info($"Anchored RMC restricted shuttle destination {ToPrettyString(uid)} on map init.");
            return;
        }

        Log.Warning($"Unable to anchor RMC restricted shuttle destination {ToPrettyString(uid)} on map init. " +
                    $"grid={xform.GridUid?.ToString() ?? "null"}, pos={xform.LocalPosition}, parent={xform.ParentUid}");
    }

    private void OnDocked(DockEvent args)
    {
        TryConfirmExpectedDock(args);
    }

    private void OnDockingPortDocked(Entity<DockingComponent> ent, ref DockEvent args)
    {
        TryConfirmExpectedDock(args);
    }

    private void TryConfirmExpectedDock(DockEvent args)
    {
        TryConfirmExpectedDock(args.GridAUid, args.GridBUid, args.DockA.Owner, args.DockB.Owner);
        TryConfirmExpectedDock(args.GridBUid, args.GridAUid, args.DockB.Owner, args.DockA.Owner);
    }

    private void TryConfirmExpectedDock(
        EntityUid candidateShuttle,
        EntityUid actualTargetGrid,
        EntityUid actualShuttleDock,
        EntityUid actualTargetDock)
    {
        if (!TryComp(candidateShuttle, out RMCExpectedDockComponent? expected))
            return;

        if (expected.Confirmed)
            return;

        expected.ActualShuttleDock = actualShuttleDock;
        expected.ActualTargetDock = actualTargetDock;

        if (IsExpectedDockPair(expected, actualTargetGrid, actualShuttleDock, actualTargetDock))
        {
            expected.FailureReason = null;
            if (AreAllExpectedDockPairsConnected(expected, out var connectedDocks, out var totalDocks))
            {
                expected.Confirmed = true;
                Log.Info($"RMC restricted docking verified: shuttle={ToPrettyString(candidateShuttle)}, " +
                         $"request={expected.RequestId}, call={expected.Call ?? "none"}, class={expected.DockingClass}, " +
                         $"shuttleDock={ToPrettyString(actualShuttleDock)}, targetDock={ToPrettyString(actualTargetDock)}, " +
                         $"connectedDocks={connectedDocks}/{totalDocks}");
                return;
            }

            Log.Info($"RMC restricted docking partially verified: shuttle={ToPrettyString(candidateShuttle)}, " +
                     $"request={expected.RequestId}, call={expected.Call ?? "none"}, class={expected.DockingClass}, " +
                     $"shuttleDock={ToPrettyString(actualShuttleDock)}, targetDock={ToPrettyString(actualTargetDock)}, " +
                     $"connectedDocks={connectedDocks}/{totalDocks}");
            return;
        }

        expected.FailureReason =
            $"unexpected dock pair: expected shuttleDock={ToPrettyString(expected.ShuttleDock)}, " +
            $"targetDock={ToPrettyString(expected.TargetDock)}, targetGrid={ToPrettyString(expected.TargetGrid)}; " +
            $"actual shuttleDock={ToPrettyString(actualShuttleDock)}, " +
            $"targetDock={ToPrettyString(actualTargetDock)}, targetGrid={ToPrettyString(actualTargetGrid)}";

        Log.Warning($"RMC restricted docking mismatch for {ToPrettyString(candidateShuttle)}: {expected.FailureReason}");
    }

    private static bool IsExpectedDockPair(
        RMCExpectedDockComponent expected,
        EntityUid actualTargetGrid,
        EntityUid actualShuttleDock,
        EntityUid actualTargetDock)
    {
        if (expected.TargetGrid != actualTargetGrid)
            return false;

        if (expected.ShuttleDock == actualShuttleDock &&
            expected.TargetDock == actualTargetDock)
        {
            return true;
        }

        if (expected.Config == null)
            return false;

        foreach (var dock in expected.Config.Docks)
        {
            if (dock.DockAUid == actualShuttleDock &&
                dock.DockBUid == actualTargetDock)
            {
                return true;
            }
        }

        return false;
    }

    private bool AreAllExpectedDockPairsConnected(
        RMCExpectedDockComponent expected,
        out int connected,
        out int total)
    {
        connected = 0;

        if (expected.Config is { Docks.Count: > 0 } config)
        {
            total = config.Docks.Count;
            foreach (var dock in config.Docks)
            {
                if (IsExpectedDockPairConnected(dock.DockAUid, dock.DockBUid))
                    connected++;
            }

            return connected == total;
        }

        total = 1;
        if (IsExpectedDockPairConnected(expected.ShuttleDock, expected.TargetDock))
            connected = 1;

        return connected == total;
    }

    private bool IsExpectedDockPairConnected(EntityUid shuttleDockUid, EntityUid targetDockUid)
    {
        return TryComp(shuttleDockUid, out DockingComponent? shuttleDock) &&
               TryComp(targetDockUid, out DockingComponent? targetDock) &&
               shuttleDock.DockedWith == targetDockUid &&
               targetDock.DockedWith == shuttleDockUid;
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
        CleanupDestinationArrival(ent);

        if (TryFailUnverifiedRestrictedDocking(ent))
            return;

        RelayToDropshipDestination(ent, args);

        var arrived = new DropshipArrivedAtDestinationEvent(ent, ent.Comp.Destination);
        RaiseLocalEvent(ref arrived);

        ent.Comp.DepartureLocation = ent.Comp.Destination;
        Dirty(ent);
    }

    private bool TryFailUnverifiedRestrictedDocking(Entity<DropshipComponent> ent)
    {
        if (!TryComp(ent, out RMCExpectedDockComponent? expected))
            return false;

        if (expected.Confirmed || TryCompleteExpectedDocking(ent, expected))
        {
            RemCompDeferred<RMCExpectedDockComponent>(ent);
            return false;
        }

        var reason = expected.FailureReason ??
                     "no matching DockEvent was received before FTL completion";

        Log.Warning($"RMC restricted docking verification failed for {ToPrettyString(ent.Owner)}: {reason}. " +
                    $"request={expected.RequestId}, call={expected.Call ?? "none"}, class={expected.DockingClass}, " +
                    $"destination={ToPrettyString(expected.Destination)}, targetGrid={ToPrettyString(expected.TargetGrid)}, " +
                    $"expectedShuttleDock={ToPrettyString(expected.ShuttleDock)}, expectedTargetDock={ToPrettyString(expected.TargetDock)}, " +
                    $"actualShuttleDock={ToPrettyString(expected.ActualShuttleDock)}, actualTargetDock={ToPrettyString(expected.ActualTargetDock)}");

        var ev = new RMCDockingVerificationFailedEvent(
            ent.Owner,
            expected.Destination,
            expected.TargetGrid,
            expected.ShuttleDock,
            expected.TargetDock,
            expected.ActualShuttleDock,
            expected.ActualTargetDock,
            expected.RequestId,
            expected.Call,
            expected.DockingClass,
            reason);

        RemCompDeferred<RMCExpectedDockComponent>(ent);
        RaiseLocalEvent(ref ev);
        return true;
    }

    private bool TryCompleteExpectedDocking(Entity<DropshipComponent> ent, RMCExpectedDockComponent expected)
    {
        if (AreAllExpectedDockPairsConnected(expected, out var connectedDocks, out var totalDocks))
        {
            expected.Confirmed = true;
            expected.ActualShuttleDock = expected.ShuttleDock;
            expected.ActualTargetDock = expected.TargetDock;
            Log.Info($"RMC restricted docking verified by dock state: shuttle={ToPrettyString(ent.Owner)}, " +
                     $"request={expected.RequestId}, call={expected.Call ?? "none"}, class={expected.DockingClass}, " +
                     $"shuttleDock={ToPrettyString(expected.ShuttleDock)}, targetDock={ToPrettyString(expected.TargetDock)}, " +
                     $"connectedDocks={connectedDocks}/{totalDocks}");
            return true;
        }

        if (TryGetUnexpectedExpectedDockOccupancy(expected, out var occupiedReason))
        {
            expected.FailureReason = occupiedReason;
            return false;
        }

        var config = expected.Config;
        if (config == null)
        {
            if (!TryComp(expected.ShuttleDock, out DockingComponent? shuttleDock))
            {
                expected.FailureReason = $"expected shuttle dock no longer exists: {ToPrettyString(expected.ShuttleDock)}";
                return false;
            }

            if (!TryComp(expected.TargetDock, out DockingComponent? targetDock))
            {
                expected.FailureReason = $"expected target dock no longer exists: {ToPrettyString(expected.TargetDock)}";
                return false;
            }

            config = _docking.GetDockingConfig(
                ent.Owner,
                expected.TargetGrid,
                expected.ShuttleDock,
                shuttleDock,
                expected.TargetDock,
                targetDock);

            if (config == null)
            {
                expected.FailureReason =
                    $"vanilla could not rebuild exact docking config after FTL arrival for shuttleDock={ToPrettyString(expected.ShuttleDock)}, " +
                    $"targetDock={ToPrettyString(expected.TargetDock)}, targetGrid={ToPrettyString(expected.TargetGrid)}";
                return false;
            }

            expected.Config = config;
        }

        var missingConfig = GetMissingExpectedDockingConfig(config);
        if (missingConfig.Docks.Count <= 0)
        {
            expected.FailureReason =
                $"expected docking config has no missing pairs but is not fully connected: connectedDocks={connectedDocks}/{totalDocks}";
            return false;
        }

        Log.Warning($"RMC restricted docking missed or partially completed vanilla arrival DockEvent; completing missing dock pairs after FTL. " +
                    $"shuttle={ToPrettyString(ent.Owner)}, request={expected.RequestId}, call={expected.Call ?? "none"}, " +
                    $"class={expected.DockingClass}, shuttleDock={ToPrettyString(expected.ShuttleDock)}, " +
                    $"targetDock={ToPrettyString(expected.TargetDock)}, storedConfig={(expected.Config != null)}, " +
                    $"connectedDocks={connectedDocks}/{totalDocks}, missingDocks={missingConfig.Docks.Count}");

        _shuttle.FTLDock((ent.Owner, Transform(ent.Owner)), missingConfig);

        if (AreAllExpectedDockPairsConnected(expected, out connectedDocks, out totalDocks))
        {
            expected.Confirmed = true;
            expected.ActualShuttleDock = expected.ShuttleDock;
            expected.ActualTargetDock = expected.TargetDock;
            return true;
        }

        expected.FailureReason =
            $"exact docking fallback ran but ports are still not fully connected: connectedDocks={connectedDocks}/{totalDocks}";
        return false;
    }

    private bool TryGetUnexpectedExpectedDockOccupancy(RMCExpectedDockComponent expected, out string reason)
    {
        if (expected.Config is { Docks.Count: > 0 } config)
        {
            foreach (var dock in config.Docks)
            {
                if (TryGetUnexpectedExpectedDockOccupancy(dock.DockAUid, dock.DockBUid, out reason))
                    return true;
            }

            reason = string.Empty;
            return false;
        }

        return TryGetUnexpectedExpectedDockOccupancy(expected.ShuttleDock, expected.TargetDock, out reason);
    }

    private bool TryGetUnexpectedExpectedDockOccupancy(
        EntityUid shuttleDockUid,
        EntityUid targetDockUid,
        out string reason)
    {
        if (!TryComp(shuttleDockUid, out DockingComponent? shuttleDock))
        {
            reason = $"expected shuttle dock no longer exists: {ToPrettyString(shuttleDockUid)}";
            return true;
        }

        if (!TryComp(targetDockUid, out DockingComponent? targetDock))
        {
            reason = $"expected target dock no longer exists: {ToPrettyString(targetDockUid)}";
            return true;
        }

        if (shuttleDock.DockedWith != null &&
            shuttleDock.DockedWith != targetDockUid)
        {
            reason =
                $"expected shuttle dock was occupied after FTL arrival: shuttleDock={ToPrettyString(shuttleDockUid)}, " +
                $"expectedTargetDock={ToPrettyString(targetDockUid)}, actualTargetDock={ToPrettyString(shuttleDock.DockedWith)}";
            return true;
        }

        if (targetDock.DockedWith != null &&
            targetDock.DockedWith != shuttleDockUid)
        {
            reason =
                $"expected target dock was occupied after FTL arrival: targetDock={ToPrettyString(targetDockUid)}, " +
                $"expectedShuttleDock={ToPrettyString(shuttleDockUid)}, actualShuttleDock={ToPrettyString(targetDock.DockedWith)}";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private DockingConfig GetMissingExpectedDockingConfig(DockingConfig config)
    {
        var missing = new DockingConfig
        {
            TargetGrid = config.TargetGrid,
            Area = config.Area,
            Coordinates = config.Coordinates,
            Angle = config.Angle,
        };

        foreach (var dock in config.Docks)
        {
            if (!IsExpectedDockPairConnected(dock.DockAUid, dock.DockBUid))
                missing.Docks.Add(dock);
        }

        return missing;
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
    }

    private void OnBeforeFTLStarted(Entity<DropshipComponent> ent, ref BeforeFTLStartedEvent args)
    {
        RelayToMountedEntities(ent, args);
    }

    private void OnRefreshUI<T>(Entity<DropshipComponent> ent, ref T args)
    {
        RefreshUI();
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

    private void OnDropshipRemoteControlToggleMsg(Entity<DropshipNavigationComputerComponent> ent, ref DropshipRemoteControlToggleMsg args)
    {
        ent.Comp.RemoteControl = !ent.Comp.RemoteControl;
        Dirty(ent, ent.Comp);
        RefreshUI();
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

    private void OnDepartureLocationFTLStarted(Entity<DropshipDestinationComponent> ent, ref DropshipRelayedEvent<FTLStartedEvent> args)
    {
        ToggleLandingLights(ent, false);
    }

    private void OnDestinationLocationFTLCompleted(Entity<DropshipDestinationComponent> ent, ref DropshipRelayedEvent<FTLCompletedEvent> args)
    {
        if (ent.Comp.Ship != args.Relayer)
            return;

        CleanupDestinationArrival(ent);
    }

    private void CleanupDestinationArrival(Entity<DropshipComponent> dropship)
    {
        if (dropship.Comp.Destination is not { } destination ||
            !TryComp(destination, out DropshipDestinationComponent? destinationComp) ||
            destinationComp.Ship != dropship.Owner)
        {
            return;
        }

        CleanupDestinationArrival((destination, destinationComp));
    }

    private void CleanupDestinationArrival(Entity<DropshipDestinationComponent> destination)
    {
        QueueDel(destination.Comp.ArrivalSoundEntity);
        destination.Comp.ArrivalSoundEntity = null;
        Dirty(destination);

        ToggleLandingLights(destination, false);
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

    public override bool FlyTo(Entity<DropshipNavigationComputerComponent> computer, EntityUid destination, EntityUid? user, bool hijack = false, float? startupTime = null, float? hyperspaceTime = null, bool offset = false)
    {
        if (user != null &&
            !CanPlayerLaunchToDestination(computer, destination, out var playerRouteReason))
        {
            if (!string.IsNullOrWhiteSpace(playerRouteReason))
                _popup.PopupEntity(playerRouteReason, computer, user.Value, PopupType.MediumCaution);

            Log.Warning($"{ToPrettyString(user.Value)} tried to launch {ToPrettyString(computer)} to route-locked destination {ToPrettyString(destination)}");
            return false;
        }

        base.FlyTo(computer, destination, user, hijack, startupTime, hyperspaceTime);

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
        RMCRestrictedDockingTravelTarget? dockingTarget = null;

        if (!hijack &&
            newDestination != null &&
            !CanUseDestinationForShuttle(computer, destination, out var reason))
        {
            Log.Warning($"{ToPrettyString(user)} tried to launch {ToPrettyString(computer)} to invalid destination {ToPrettyString(destination)} ({reason})");
            return false;
        }

        if (!hijack &&
            newDestination != null &&
            computer.Comp.RequiresRestrictedDestination &&
            newDestination.Ship != dropshipId &&
            TryComp(destination, out DockingComponent? destinationDock))
        {
            if (!TryGetDockingTravelTarget(computer, destination, destinationDock, out var travelTarget, out reason))
            {
                Log.Warning($"{ToPrettyString(user)} tried to launch {ToPrettyString(computer)} to invalid docking destination {ToPrettyString(destination)} ({reason})");
                return false;
            }

            dockingTarget = travelTarget;
        }

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

        if (TryComp(dropship.Destination, out DropshipDestinationComponent? oldDestination))
        {
            SetDestinationShip((dropship.Destination.Value, oldDestination), null);
        }

        if (newDestination != null)
        {
            SetDestinationShip((destination, newDestination), dropshipId);
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

        if (dockingTarget is { } docking)
        {
            destCoords = docking.Config.Coordinates;
            rotation = docking.Config.Angle;
        }
        else
        {
            if (TryComp(dropshipId, out PhysicsComponent? physics))
            {
                _physics.SetLocalCenter(dropshipId.Value, physics, Vector2.Zero);
                destCoords = destCoords.Offset(-physics.LocalCenter);
            }

            if (offset)
                destCoords = destCoords.Offset(new Vector2(-0.5f, -0.5f));
        }

        if (dockingTarget is { } target)
            StartRestrictedDockingFTL(dropshipId.Value, shuttleComp, computer, destination, target, startupTime, hyperspaceTime);
        else
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

        return true;
    }

    private bool TryGetDockingTravelTarget(
        Entity<DropshipNavigationComputerComponent> computer,
        EntityUid destination,
        DockingComponent destinationDock,
        out RMCRestrictedDockingTravelTarget target,
        out string reason)
    {
        target = default;

        if (Transform(computer).GridUid is not { } shuttle)
        {
            reason = Loc.GetString("rmc-dropship-no-compatible-docking-port");
            Log.Warning($"RMC restricted docking target rejected: computer={ToPrettyString(computer)}, " +
                        $"destination={ToPrettyString(destination)}, reason=computer is not on a shuttle grid");
            return false;
        }

        if (Transform(destination).GridUid is not { } targetGrid)
        {
            reason = Loc.GetString("rmc-dropship-no-compatible-docking-port");
            Log.Warning($"RMC restricted docking target rejected: computer={ToPrettyString(computer)}, " +
                        $"destination={ToPrettyString(destination)}, reason=destination is not on a grid");
            return false;
        }

        var allShuttleDocks = _docking.GetDocks(shuttle)
            .OrderBy(dock => dock.Owner.GetHashCode())
            .ToList();
        var preferredDocks = allShuttleDocks
            .Where(dock => HasComp<RMCShuttleMobileDockComponent>(dock.Owner))
            .ToList();
        var shuttleDocks = GetRestrictedShuttleDockCandidates(allShuttleDocks, preferredDocks);
        var targetDocks = GetRestrictedDockingTargetCluster(destination, destinationDock, targetGrid);

        var usingPreferredDocks = preferredDocks.Count > 0;
        var targetGridAngle = _transform.GetWorldRotation(targetGrid).Reduced();
        var attempts = new List<string>();
        RMCRestrictedDockingCandidate? bestCandidate = null;

        foreach (var shuttleDock in shuttleDocks)
        {
            foreach (var targetDock in targetDocks)
            {
                var pairConfig = _docking.GetDockingConfig(
                    shuttle,
                    targetGrid,
                    shuttleDock.Owner,
                    shuttleDock.Comp,
                    targetDock.Owner,
                    targetDock.Comp);

                if (pairConfig == null)
                {
                    attempts.Add($"{DescribeDockForLog(shuttleDock.Owner, shuttleDock.Comp)} -> {DescribeDockForLog(targetDock.Owner, targetDock.Comp)} null");
                    continue;
                }

                var config = BuildRestrictedDockingConfig(
                    shuttle,
                    targetGrid,
                    shuttleDock,
                    targetDock,
                    pairConfig,
                    shuttleDocks,
                    targetDocks);

                if (!TryGetRestrictedDockingAnchor(config, destination, out var anchorShuttleDock, out var anchorTargetDock))
                {
                    attempts.Add($"{DescribeDockForLog(shuttleDock.Owner, shuttleDock.Comp)} -> {DescribeDockForLog(targetDock.Owner, targetDock.Comp)} " +
                                 $"no config containing selected destination");
                    continue;
                }

                var stagedCoordinates = GetRestrictedDockingStagedGridCoordinates(anchorShuttleDock, anchorTargetDock, config);
                if (TryGetRestrictedDockingObstacle(shuttle, targetGrid, destination, stagedCoordinates, config.Angle, out var obstacle))
                {
                    attempts.Add($"{DescribeDockForLog(shuttleDock.Owner, shuttleDock.Comp)} -> {DescribeDockForLog(targetDock.Owner, targetDock.Comp)} " +
                                 $"blocked by {DescribeRestrictedDockingObstacle(obstacle)} docks={config.Docks.Count} " +
                                 $"exactCoords={config.Coordinates} stagedCoords={stagedCoordinates} angle={config.Angle}");
                    continue;
                }

                var candidate = new RMCRestrictedDockingCandidate(
                    config,
                    anchorShuttleDock,
                    anchorTargetDock,
                    stagedCoordinates);

                attempts.Add($"{DescribeDockForLog(shuttleDock.Owner, shuttleDock.Comp)} -> {DescribeDockForLog(targetDock.Owner, targetDock.Comp)} " +
                             $"valid docks={config.Docks.Count} exactCoords={config.Coordinates} stagedCoords={stagedCoordinates} angle={config.Angle}");

                if (bestCandidate is not { } best ||
                    IsBetterRestrictedDockingCandidate(candidate, best, targetGridAngle))
                {
                    bestCandidate = candidate;
                }
            }
        }

        if (bestCandidate is { } selected)
        {
            Log.Info($"RMC restricted docking config selected: computer={ToPrettyString(computer)}, " +
                     $"shuttle={ToPrettyString(shuttle)}, destination={ToPrettyString(destination)}, " +
                     $"targetGrid={ToPrettyString(targetGrid)}, anchorShuttleDock={ToPrettyString(selected.ShuttleDock)}, " +
                     $"anchorTargetDock={ToPrettyString(selected.TargetDock)}, dockCount={selected.Config.Docks.Count}, " +
                     $"shuttleDocks=[{string.Join(" | ", shuttleDocks.Select(d => DescribeDockForLog(d.Owner, d.Comp)))}], " +
                     $"targetDocks=[{string.Join(" | ", targetDocks.Select(d => DescribeDockForLog(d.Owner, d.Comp)))}], " +
                     $"attempts=[{string.Join(" | ", attempts)}], " +
                     $"exactCoords={selected.Config.Coordinates}, stagedCoords={selected.StagedCoordinates}, angle={selected.Config.Angle}");

            target = new RMCRestrictedDockingTravelTarget(
                selected.Config,
                shuttle,
                targetGrid,
                destination,
                selected.ShuttleDock,
                selected.TargetDock);
            reason = string.Empty;
            return true;
        }

        Log.Warning($"RMC restricted docking config failed: computer={ToPrettyString(computer)}, " +
                    $"shuttle={ToPrettyString(shuttle)}, destination={ToPrettyString(destination)}, " +
                    $"targetGrid={ToPrettyString(targetGrid)}, targetDock={DescribeDockForLog(destination, destinationDock)}, " +
                    $"totalShuttleDocks={allShuttleDocks.Count}, selectedShuttleDocks={shuttleDocks.Count}, " +
                    $"preferredOnly={usingPreferredDocks}, targetClusterDocks={targetDocks.Count}, " +
                    $"shuttleDocks=[{string.Join(" | ", shuttleDocks.Select(d => DescribeDockForLog(d.Owner, d.Comp)))}], " +
                    $"targetDocks=[{string.Join(" | ", targetDocks.Select(d => DescribeDockForLog(d.Owner, d.Comp)))}], " +
                    $"attempts=[{string.Join(" | ", attempts)}]");

        reason = Loc.GetString("rmc-dropship-no-compatible-docking-port");
        return false;
    }

    private List<Entity<DockingComponent>> GetRestrictedShuttleDockCandidates(
        List<Entity<DockingComponent>> allShuttleDocks,
        List<Entity<DockingComponent>> preferredDocks)
    {
        if (preferredDocks.Count != 1)
            return preferredDocks.Count > 0 ? preferredDocks : allShuttleDocks;

        var preferred = preferredDocks[0];
        if (!_xformQuery.TryComp(preferred.Owner, out var preferredXform))
            return preferredDocks;

        var candidates = allShuttleDocks
            .Where(dock =>
            {
                if (!_xformQuery.TryComp(dock.Owner, out var dockXform))
                    return false;

                return (dockXform.LocalPosition - preferredXform.LocalPosition).LengthSquared() <=
                       RestrictedDockingShuttleClusterRadius * RestrictedDockingShuttleClusterRadius;
            })
            .ToList();

        return candidates.Count > 0 ? candidates : preferredDocks;
    }

    private List<Entity<DockingComponent>> GetRestrictedDockingTargetCluster(
        EntityUid destination,
        DockingComponent destinationDock,
        EntityUid targetGrid)
    {
        var targetDocks = new List<Entity<DockingComponent>>
        {
            (destination, destinationDock),
        };

        if (!TryComp(destination, out DropshipDestinationComponent? destinationComp) ||
            !_xformQuery.TryComp(destination, out var destinationXform))
        {
            return targetDocks;
        }

        foreach (var dock in _docking.GetDocks(targetGrid).OrderBy(dock => dock.Owner.GetHashCode()))
        {
            if (dock.Owner == destination ||
                !_xformQuery.TryComp(dock.Owner, out var dockXform) ||
                dockXform.GridUid != targetGrid ||
                (dockXform.LocalPosition - destinationXform.LocalPosition).LengthSquared() >
                RestrictedDockingClusterRadius * RestrictedDockingClusterRadius)
            {
                continue;
            }

            if (!TryComp(dock.Owner, out DropshipDestinationComponent? dockDestination) ||
                !IsRestrictedDockingClusterMatch(destinationComp, dockDestination))
            {
                continue;
            }

            targetDocks.Add(dock);
        }

        return targetDocks;
    }

    private static bool IsRestrictedDockingClusterMatch(
        DropshipDestinationComponent destination,
        DropshipDestinationComponent candidate)
    {
        if (!candidate.Enabled ||
            candidate.Reserved != destination.Reserved)
        {
            return false;
        }

        if (destination.LandingClasses.Count > 0 &&
            !destination.LandingClasses.Any(candidate.LandingClasses.Contains))
        {
            return false;
        }

        if (destination.LandingTags.Count > 0 &&
            !destination.LandingTags.Any(candidate.LandingTags.Contains))
        {
            return false;
        }

        return true;
    }

    private DockingConfig BuildRestrictedDockingConfig(
        EntityUid shuttle,
        EntityUid targetGrid,
        Entity<DockingComponent> anchorShuttleDock,
        Entity<DockingComponent> anchorTargetDock,
        DockingConfig anchorConfig,
        List<Entity<DockingComponent>> shuttleDocks,
        List<Entity<DockingComponent>> targetDocks)
    {
        var config = new DockingConfig
        {
            Docks = new List<(EntityUid DockAUid, EntityUid DockBUid, DockingComponent DockA, DockingComponent DockB)>
            {
                (anchorShuttleDock.Owner, anchorTargetDock.Owner, anchorShuttleDock.Comp, anchorTargetDock.Comp),
            },
            TargetGrid = anchorConfig.TargetGrid,
            Area = anchorConfig.Area,
            Coordinates = anchorConfig.Coordinates,
            Angle = anchorConfig.Angle,
        };

        var usedShuttleDocks = new HashSet<EntityUid> { anchorShuttleDock.Owner };
        var usedTargetDocks = new HashSet<EntityUid> { anchorTargetDock.Owner };

        foreach (var shuttleDock in shuttleDocks)
        {
            if (usedShuttleDocks.Contains(shuttleDock.Owner))
                continue;

            foreach (var targetDock in targetDocks)
            {
                if (usedTargetDocks.Contains(targetDock.Owner))
                    continue;

                var otherConfig = _docking.GetDockingConfig(
                    shuttle,
                    targetGrid,
                    shuttleDock.Owner,
                    shuttleDock.Comp,
                    targetDock.Owner,
                    targetDock.Comp);

                if (otherConfig == null ||
                    !IsSameRestrictedDockingPlacement(anchorConfig, otherConfig))
                {
                    continue;
                }

                config.Docks.Add((shuttleDock.Owner, targetDock.Owner, shuttleDock.Comp, targetDock.Comp));
                usedShuttleDocks.Add(shuttleDock.Owner);
                usedTargetDocks.Add(targetDock.Owner);
                break;
            }
        }

        return config;
    }

    private static bool IsSameRestrictedDockingPlacement(DockingConfig anchor, DockingConfig candidate)
    {
        return anchor.Angle.Equals(candidate.Angle) &&
               anchor.Area.Equals(candidate.Area);
    }

    private static bool IsBetterRestrictedDockingCandidate(
        RMCRestrictedDockingCandidate candidate,
        RMCRestrictedDockingCandidate current,
        Angle targetGridAngle)
    {
        if (candidate.Config.Docks.Count != current.Config.Docks.Count)
            return candidate.Config.Docks.Count > current.Config.Docks.Count;

        var candidateAngleDistance = Math.Abs(Angle.ShortestDistance(candidate.Config.Angle.Reduced(), targetGridAngle).Theta);
        var currentAngleDistance = Math.Abs(Angle.ShortestDistance(current.Config.Angle.Reduced(), targetGridAngle).Theta);
        return candidateAngleDistance < currentAngleDistance;
    }

    private static bool TryGetRestrictedDockingAnchor(
        DockingConfig config,
        EntityUid destination,
        out EntityUid shuttleDock,
        out EntityUid targetDock)
    {
        foreach (var dock in config.Docks)
        {
            if (dock.DockBUid != destination)
                continue;

            shuttleDock = dock.DockAUid;
            targetDock = dock.DockBUid;
            return true;
        }

        shuttleDock = default;
        targetDock = default;
        return false;
    }

    private bool TryGetRestrictedDockingObstacle(
        EntityUid shuttle,
        EntityUid targetGrid,
        EntityUid destination,
        EntityCoordinates coordinates,
        Angle angle,
        out EntityUid obstacle)
    {
        obstacle = default;

        if (!_fixturesQuery.TryComp(shuttle, out var fixtures))
            return false;

        var transform = new Transform(coordinates.Position, angle);
        foreach (var fixture in fixtures.Fixtures.Values)
        {
            if (!fixture.Hard)
                continue;

            _restrictedDockingObstacles.Clear();
            _entityLookup.GetLocalEntitiesIntersecting(
                targetGrid,
                fixture.Shape,
                transform,
                _restrictedDockingObstacles,
                flags: LookupFlags.Uncontained);

            foreach (var ent in _restrictedDockingObstacles)
            {
                if (ent == shuttle ||
                    ent == destination ||
                    HasComp<AreaComponent>(ent) ||
                    _ftlSmashImmuneQuery.HasComp(ent))
                {
                    continue;
                }

                if (!_xformQuery.TryComp(ent, out var xform) ||
                    xform.GridUid != targetGrid ||
                    !xform.Anchored)
                {
                    continue;
                }

                if (!_physicsQuery.TryComp(ent, out var physics) ||
                    !physics.CanCollide ||
                    !physics.Hard)
                {
                    continue;
                }

                obstacle = ent;
                return true;
            }
        }

        return false;
    }

    private EntityCoordinates GetRestrictedDockingStagedGridCoordinates(
        EntityUid shuttleDock,
        EntityUid targetDock,
        DockingConfig config)
    {
        if (RestrictedDockingStagingOffset <= 0 ||
            !_xformQuery.TryComp(shuttleDock, out var shuttleDockXform) ||
            !_xformQuery.TryComp(targetDock, out var targetDockXform))
        {
            return config.Coordinates;
        }

        var finalShuttleDockPosition = config.Coordinates.Position +
                                       config.Angle.RotateVec(shuttleDockXform.LocalPosition);
        var awayFromTarget = finalShuttleDockPosition - targetDockXform.LocalPosition;

        if (awayFromTarget.LengthSquared() < 0.0001f)
            awayFromTarget = targetDockXform.LocalRotation.RotateVec(new Vector2(0f, -1f));

        if (awayFromTarget.LengthSquared() < 0.0001f)
            return config.Coordinates;

        awayFromTarget = Vector2.Normalize(awayFromTarget);
        return config.Coordinates.Offset(awayFromTarget * RestrictedDockingStagingOffset);
    }

    private string DescribeDockForLog(EntityUid uid, DockingComponent dock)
    {
        var xform = Transform(uid);
        var proto = MetaData(uid).EntityPrototype?.ID ?? "none";
        return $"{ToPrettyString(uid)} proto:{proto} grid:{ToPrettyString(xform.GridUid)} " +
               $"pos:{xform.LocalPosition} rot:{xform.LocalRotation} anchored:{xform.Anchored} " +
               $"docked:{dock.Docked} dockedWith:{ToPrettyString(dock.DockedWith)} " +
               $"mobile:{HasComp<RMCShuttleMobileDockComponent>(uid)}";
    }

    private string DescribeRestrictedDockingObstacle(EntityUid uid)
    {
        var xform = Transform(uid);
        var proto = MetaData(uid).EntityPrototype?.ID ?? "none";
        return $"{ToPrettyString(uid)} proto:{proto} grid:{ToPrettyString(xform.GridUid)} " +
               $"pos:{xform.LocalPosition} rot:{xform.LocalRotation}";
    }

    private void StartRestrictedDockingFTL(
        EntityUid dropshipId,
        ShuttleComponent shuttle,
        Entity<DropshipNavigationComputerComponent> computer,
        EntityUid destination,
        RMCRestrictedDockingTravelTarget target,
        float? startupTime,
        float? hyperspaceTime)
    {
        var exactCoordinates = target.Config.Coordinates;
        var exactAngle = target.Config.Angle;
        var stagedGridCoordinates = GetRestrictedDockingStagedGridCoordinates(
            target.ShuttleDock,
            target.TargetDock,
            target.Config);
        var stagedMapCoordinates = _transform.ToMapCoordinates(stagedGridCoordinates);
        var stagedMap = _mapSystem.GetMap(stagedMapCoordinates.MapId);
        var stagedCoordinates = new EntityCoordinates(stagedMap, stagedMapCoordinates.Position);
        var stagedAngle = exactAngle + _transform.GetWorldRotation(exactCoordinates.EntityId);

        var expected = EnsureComp<RMCExpectedDockComponent>(dropshipId);
        expected.Destination = destination;
        expected.TargetGrid = target.TargetGrid;
        expected.ShuttleDock = target.ShuttleDock;
        expected.TargetDock = target.TargetDock;
        expected.Coordinates = exactCoordinates;
        expected.Angle = exactAngle;
        expected.Config = target.Config;
        expected.DockingClass = computer.Comp.ShuttleDockingClass;
        expected.Confirmed = false;
        expected.ActualShuttleDock = null;
        expected.ActualTargetDock = null;
        expected.FailureReason = null;

        if (TryComp(dropshipId, out RMCRestrictedShuttleComponent? restrictedShuttle))
        {
            expected.RequestId = restrictedShuttle.RequestId;
            expected.Call = restrictedShuttle.Call;
        }
        else
        {
            expected.RequestId = Guid.Empty;
            expected.Call = null;
        }

        Log.Info($"RMC restricted docking FTL started: shuttle={ToPrettyString(dropshipId)}, " +
                 $"request={expected.RequestId}, call={expected.Call ?? "none"}, class={expected.DockingClass}, " +
                 $"destination={ToPrettyString(destination)}, targetGrid={ToPrettyString(target.TargetGrid)}, " +
                 $"shuttleDock={ToPrettyString(target.ShuttleDock)}, targetDock={ToPrettyString(target.TargetDock)}, " +
                 $"dockCount={target.Config.Docks.Count}, exactCoordinates={exactCoordinates}, exactAngle={exactAngle}, " +
                 $"stagedCoordinates={stagedCoordinates}, stagedAngle={stagedAngle}, stagingOffset={RestrictedDockingStagingOffset}");

        _shuttle.FTLToCoordinates(
            dropshipId,
            shuttle,
            stagedCoordinates,
            stagedAngle,
            startupTime: startupTime,
            hyperspaceTime: hyperspaceTime);
    }

    private readonly record struct RMCRestrictedDockingCandidate(
        DockingConfig Config,
        EntityUid ShuttleDock,
        EntityUid TargetDock,
        EntityCoordinates StagedCoordinates);

    private readonly record struct RMCRestrictedDockingTravelTarget(
        DockingConfig Config,
        EntityUid Shuttle,
        EntityUid TargetGrid,
        EntityUid Destination,
        EntityUid ShuttleDock,
        EntityUid TargetDock);

    protected override void RefreshUI(Entity<DropshipNavigationComputerComponent> computer)
    {
        if (!_ui.IsUiOpen(computer.Owner, DropshipNavigationUiKey.Key))
            return;

        if (Transform(computer).GridUid is not { } grid)
            return;

        var doorLockStatus = GetDoorLockStatus(grid);

        if (!TryComp(grid, out FTLComponent? ftl) ||
            !ftl.Running ||
            ftl.State == FTLState.Available)
        {
            NetEntity? flyBy = null;
            var destinations = new List<Destination>();
            if (computer.Comp.PlayerDestinationLockEnabled)
            {
                if (computer.Comp.PlayerAllowedDestination is { } allowed &&
                    TryComp(allowed, out DropshipDestinationComponent? allowedComp) &&
                    IsDestinationAllowed(computer, allowed, out _))
                {
                    destinations.Add(new Destination(
                        GetNetEntity(allowed),
                        Name(allowed),
                        allowedComp.Ship != null && allowedComp.Ship != grid,
                        HasComp<PrimaryLandingZoneComponent>(allowed)));
                }
            }
            else
            {
                var query = EntityQueryEnumerator<DropshipDestinationComponent>();
                while (query.MoveNext(out var uid, out var comp))
                {
                    var netDestination = GetNetEntity(uid);
                    if (comp.Ship == grid)
                    {
                        flyBy = netDestination;
                        continue;
                    }

                    if (!IsDestinationAllowed(computer, uid, out _))
                        continue;

                    var destination = new Destination(
                        netDestination,
                        Name(uid),
                        comp.Ship != null,
                        HasComp<PrimaryLandingZoneComponent>(uid)
                    );
                    destinations.Add(destination);
                }
            }

            var state = new DropshipNavigationDestinationsBuiState(flyBy, destinations, doorLockStatus, computer.Comp.RemoteControl, computer.Comp.LaunchAlarmStatus);
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

        var travelState = new DropshipNavigationTravellingBuiState(ftl.State, ftl.StateTime, destinationName, departureName, doorLockStatus, computer.Comp.RemoteControl, computer.Comp.LaunchAlarmStatus);
        _ui.SetUiState(computer.Owner, DropshipNavigationUiKey.Key, travelState);
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

    public void UnlockDoor(Entity<DoorBoltComponent?> door, bool forceSafetyUnlock = false)
    {
        if (HasComp<RMCDropshipDoorConsoleLockComponent>(door.Owner))
            return;

        if (!forceSafetyUnlock &&
            TryComp(door.Owner, out RMCDockingSafetyLockedComponent? safetyLock) &&
            safetyLock.Active)
        {
            return;
        }

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
