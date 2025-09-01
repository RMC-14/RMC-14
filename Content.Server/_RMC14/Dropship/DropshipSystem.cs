using System.Numerics;
using Content.Server._RMC14.Marines;
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
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction;
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
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Dropship;

public sealed class DropshipSystem : SharedDropshipSystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DoorSystem _door = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedXenoAnnounceSystem _xenoAnnounce = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _rmcFlammable = default!;
    [Dependency] private readonly SharedRMCExplosionSystem _rmcExplosion = default!;
    [Dependency] private readonly RMCAlertLevelSystem _alertLevelSystem = default!;
    [Dependency] private readonly AreaSystem _area = default!;

    private EntityQuery<DockingComponent> _dockingQuery;
    private EntityQuery<DoorComponent> _doorQuery;
    private EntityQuery<DoorBoltComponent> _doorBoltQuery;

    private TimeSpan _lzPrimaryAutoDelay;
    private TimeSpan _flyByTime;
    private TimeSpan _hijackTravelTime;

    private EntityUid _dropshipId;
    private bool _hijack;

    public override void Initialize()
    {
        base.Initialize();

        _dockingQuery = GetEntityQuery<DockingComponent>();
        _doorQuery = GetEntityQuery<DoorComponent>();
        _doorBoltQuery = GetEntityQuery<DoorBoltComponent>();

        SubscribeLocalEvent<DropshipNavigationComputerComponent, ActivateInWorldEvent>(OnActivateInWorld);

        SubscribeLocalEvent<DropshipComponent, FTLRequestEvent>(OnRefreshUI);
        SubscribeLocalEvent<DropshipComponent, FTLStartedEvent>(OnFTLStarted);
        SubscribeLocalEvent<DropshipComponent, FTLCompletedEvent>(OnFTLCompleted);
        SubscribeLocalEvent<DropshipComponent, FTLUpdatedEvent>(OnFTLUpdated);

        SubscribeLocalEvent<DropshipInFlyByComponent, FTLCompletedEvent>(OnInFlyByFTLCompleted);

        Subs.BuiEvents<DropshipNavigationComputerComponent>(DropshipNavigationUiKey.Key,
            subs =>
            {
                subs.Event<DropshipLockdownMsg>(OnDropshipNavigationLockdownMsg);
            });

        Subs.CVar(_config, RMCCVars.RMCLandingZonePrimaryAutoMinutes, v => _lzPrimaryAutoDelay = TimeSpan.FromMinutes(v), true);
        Subs.CVar(_config, RMCCVars.RMCDropshipFlyByTimeSeconds, v => _flyByTime = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_config, RMCCVars.RMCDropshipHijackTravelTimeSeconds, v => _hijackTravelTime = TimeSpan.FromSeconds(v), true);
    }

    private void OnActivateInWorld(Entity<DropshipNavigationComputerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!HasComp<DropshipHijackerComponent>(args.User))
            return;

        if (!TryDropshipHijackPopup(ent, args.User, false))
            return;

        if (TryComp(ent, out TransformComponent? xform) &&
            TryComp(xform.ParentUid, out DropshipComponent? dropship) &&
            dropship.Crashed)
        {
            return;
        }

        args.Handled = true;

        var destinations = new List<(NetEntity Id, string Name)>();
        var query = EntityQueryEnumerator<DropshipHijackDestinationComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            destinations.Add((GetNetEntity(uid), Name(uid)));
        }

        _ui.OpenUi(ent.Owner, DropshipHijackerUiKey.Key, args.User);
        _ui.SetUiState(ent.Owner, DropshipHijackerUiKey.Key, new DropshipHijackerBuiState(destinations));
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

        ent.Comp.DepartureLocation = ent.Comp.Destination;
        Dirty(ent);
    }

    private void OnFTLUpdated(Entity<DropshipComponent> ent, ref FTLUpdatedEvent args)
    {
        if (TryComp(ent, out FTLComponent? ftl))
        {
            ent.Comp.State = ftl.State;
            Dirty(ent);
        }

        RefreshUI();
    }

    private void OnRefreshUI<T>(Entity<DropshipComponent> ent, ref T args)
    {
        RefreshUI();
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
            ftl.State is FTLState.Travelling or FTLState.Arriving)
        {
            return;
        }

        var time = _timing.CurTime;
        if (time < dropship.LastLocked + dropship.LockCooldown)
            return;

        dropship.Locked = !dropship.Locked;
        dropship.LastLocked = time;
        SetAllDocks(grid, dropship.Locked);
    }

    public override bool FlyTo(Entity<DropshipNavigationComputerComponent> computer, EntityUid destination, EntityUid? user, bool hijack = false, float? startupTime = null, float? hyperspaceTime = null, bool offset = false)
    {
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
        else if (!hijack && newDestination != null && newDestination.Ship != null)
        {
            Log.Warning($"{ToPrettyString(user)} tried to launch to occupied dropship destination {ToPrettyString(destination)}");
        }

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
                _audio.PlayPvs(dropship.LocalHijackSound, dropshipId.Value);

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

    protected override void RefreshUI(Entity<DropshipNavigationComputerComponent> computer)
    {
        if (!_ui.IsUiOpen(computer.Owner, DropshipNavigationUiKey.Key))
            return;

        if (Transform(computer).GridUid is not { } grid)
            return;

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

            var state = new DropshipNavigationDestinationsBuiState(flyBy, destinations);
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

        var travelState = new DropshipNavigationTravellingBuiState(ftl.State, ftl.StateTime, destinationName, departureName);
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

    private void SetAllDocks(EntityUid dropship, bool locked)
    {
        var enumerator = Transform(dropship).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            if (!_dockingQuery.HasComp(child))
                continue;

            if (locked)
                LockDoor(child);
            else
                UnlockDoor(child);
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

                _audio.PlayGlobal(dropship.IncomingSound, destinationFilter, true);
                continue;
            }

            if (dropship.HijackLandAt - dropship.ExplodeTime <= time && !dropship.DidExplosion)
            {
                dropship.DidExplosion = true;
                Dirty(uid, dropship);

                _audio.PlayGlobal(dropship.CrashSound, destinationFilter, true);
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
