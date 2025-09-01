using System.Linq;
using System.Numerics;
using System.Text;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.HyperSleep;
using Content.Shared._RMC14.Power;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared.Audio;
using Content.Shared.CCVar;
using Content.Shared.Coordinates;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Evacuation;

public abstract class SharedEvacuationSystem : EntitySystem
{
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly SharedHyperSleepChamberSystem _hyperSleep = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedRMCExplosionSystem _rmcExplosion = default!;
    [Dependency] private readonly SharedRMCPowerSystem _rmcPower = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedXenoAnnounceSystem _xenoAnnounce = default!;

    private EntityQuery<AreaComponent> _areaQuery;
    private EntityQuery<DoorComponent> _doorQuery;
    private EntityQuery<MobStateComponent> _mobStateQuery;

    private MapId? _map;
    private int _index;

    public override void Initialize()
    {
        _areaQuery = GetEntityQuery<AreaComponent>();
        _doorQuery = GetEntityQuery<DoorComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();

        SubscribeLocalEvent<DropshipHijackLandedEvent>(OnDropshipHijackLanded, after: [typeof(SharedRMCPowerSystem)]);
        SubscribeLocalEvent<EvacuationEnabledEvent>(OnEvacuationEnabled);
        SubscribeLocalEvent<EvacuationDisabledEvent>(OnEvacuationDisabled);
        SubscribeLocalEvent<EvacuationProgressEvent>(OnEvacuationProgress);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<GridSpawnerComponent, MapInitEvent>(OnGridSpawnerMapInit);

        SubscribeLocalEvent<EvacuationDoorComponent, BeforeDoorOpenedEvent>(OnEvacuationDoorBeforeOpened);
        SubscribeLocalEvent<EvacuationDoorComponent, BeforeDoorClosedEvent>(OnEvacuationDoorBeforeClosed);
        SubscribeLocalEvent<EvacuationDoorComponent, BeforePryEvent>(OnEvacuationDoorBeforePry);

        SubscribeLocalEvent<EvacuationComputerComponent, ExaminedEvent>(OnEvacuationComputerExamined);
        SubscribeLocalEvent<EvacuationComputerComponent, ActivatableUIOpenAttemptEvent>(OnEvacuationComputerUIOpenAttempt);

        SubscribeLocalEvent<LifeboatComputerComponent, ActivatableUIOpenAttemptEvent>(OnLifeboatComputerUIOpenAttempt);

        SubscribeLocalEvent<EvacuationPumpComponent, ExaminedEvent>(OnEvacuationPumpExamined);

        Subs.BuiEvents<EvacuationComputerComponent>(EvacuationComputerUi.Key,
            subs =>
            {
                subs.Event<EvacuationComputerLaunchBuiMsg>(OnEvacuationComputerLaunch);
            });

        Subs.BuiEvents<LifeboatComputerComponent>(LifeboatComputerUi.Key,
            subs =>
            {
                subs.Event<LifeboatComputerLaunchBuiMsg>(OnLifeboatComputerLaunch);
            });
    }

    private void OnDropshipHijackLanded(ref DropshipHijackLandedEvent ev)
    {
        var evacuationProgress = EnsureComp<EvacuationProgressComponent>(ev.Map);
        evacuationProgress.DropShipCrashed = true;

        var doors = EntityQueryEnumerator<EvacuationDoorComponent>();
        while (doors.MoveNext(out var uid, out var door))
        {
            door.Locked = false;
            Dirty(uid, door);
        }

        _config.SetCVar(CCVars.GameDisallowLateJoins, true);
    }

    private void OnEvacuationEnabled(ref EvacuationEnabledEvent ev)
    {
        var lifeboats = EntityQueryEnumerator<LifeboatComputerComponent>();
        while (lifeboats.MoveNext(out var uid, out var computer))
        {
            computer.Enabled = true;
            Dirty(uid, computer);
        }

        var evacuation = EntityQueryEnumerator<EvacuationComputerComponent>();
        while (evacuation.MoveNext(out var computerId, out var computer))
        {
            if (computer.Mode == EvacuationComputerMode.Disabled)
            {
                computer.Mode = EvacuationComputerMode.Ready;
                Dirty(computerId, computer);
            }
        }
    }

    private void OnEvacuationDisabled(ref EvacuationDisabledEvent ev)
    {
        var lifeboats = EntityQueryEnumerator<LifeboatComputerComponent>();
        while (lifeboats.MoveNext(out var uid, out var computer))
        {
            computer.Enabled = false;
            Dirty(uid, computer);
        }
    }

    private void OnEvacuationProgress(ref EvacuationProgressEvent ev)
    {
        var evacuation = EntityQueryEnumerator<EvacuationComputerComponent>();
        while (evacuation.MoveNext(out var computerId, out var computer))
        {
            if (computer.Mode == EvacuationComputerMode.Disabled)
            {
                computer.Mode = EvacuationComputerMode.Ready;
                Dirty(computerId, computer);
            }
        }
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _map = null;
        _index = 0;
    }

    private void OnGridSpawnerMapInit(Entity<GridSpawnerComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Spawn is not { } spawn)
            return;

        if (_net.IsClient)
            return;

        if (!_config.GetCVar(CCVars.GridFill))
            return;

        if (_map == null)
        {
            _mapSystem.CreateMap(out var mapId);
            _map = mapId;
        }

        var offset = new Vector2(_index * 50, _index * 50);
        _index++;

        if (!_mapSystem.MapExists(_map) ||
            !_mapLoader.TryLoadGrid(_map.Value, spawn, out var result, offset: offset))
        {
            return;
        }

        var grid = result.Value;
        var xform = Transform(ent);
        var coordinates = _transform.GetMapCoordinates(ent, xform);
        coordinates = coordinates.Offset(ent.Comp.Offset);
        _transform.SetMapCoordinates(grid, coordinates);

        if (TryComp(grid, out PhysicsComponent? physics) &&
            TryComp(grid, out FixturesComponent? fixtures))
        {
            _physics.SetBodyType(grid, BodyType.Static, manager: fixtures, body: physics);
            _physics.SetBodyStatus(grid, physics, BodyStatus.OnGround);
            _physics.SetFixedRotation(grid, true, manager: fixtures, body: physics);
        }
    }

    private void OnEvacuationDoorBeforeOpened(Entity<EvacuationDoorComponent> ent, ref BeforeDoorOpenedEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.Locked)
            args.Cancel();
    }

    private void OnEvacuationDoorBeforeClosed(Entity<EvacuationDoorComponent> ent, ref BeforeDoorClosedEvent args)
    {
        if (ent.Comp.Locked)
            args.PerformCollisionCheck = false;
    }

    private void OnEvacuationDoorBeforePry(Entity<EvacuationDoorComponent> ent, ref BeforePryEvent args)
    {
        if (ent.Comp.Locked)
            args.Cancelled = true;
    }

    private void OnEvacuationComputerExamined(Entity<EvacuationComputerComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.MaxMobs is { } maxMobs)
        {
            using (args.PushGroup(nameof(EvacuationComputerComponent)))
            {
                args.PushMarkup($"[color=red]This pod is only rated for a maximum of {maxMobs} occupants! Any more may cause it to crash and burn.[/color]");
            }
        }
    }

    private void OnEvacuationComputerUIOpenAttempt(Entity<EvacuationComputerComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled || ent.Comp.Mode == EvacuationComputerMode.Ready)
            return;

        args.Cancel();

        var msg = ent.Comp.Mode switch
        {
            EvacuationComputerMode.Disabled => "Evacuation has not started.",
            EvacuationComputerMode.Ready => "",
            EvacuationComputerMode.Travelling => "The escape pod has already been launched!",
            EvacuationComputerMode.Crashed => "This escape pod has crashed!",
            _ => throw new ArgumentOutOfRangeException(),
        };

        _popup.PopupClient(msg, ent, args.User, PopupType.SmallCaution);
    }

    private void OnEvacuationPumpExamined(Entity<EvacuationPumpComponent> ent, ref ExaminedEvent args)
    {
        if (!IsEvacuationInProgress())
            return;
        using (args.PushGroup(nameof(EvacuationPumpComponent)))
        {
            var progress = GetEvacuationProgress();
            if (progress < 25)
                args.PushMarkup("It looks like it barely has any fuel yet.");
            else if (progress < 50)
                args.PushMarkup("It looks like it has accumulated some fuel.");
            else if (progress < 75)
                args.PushMarkup("It looks like the fuel tank is a little over half full.");
            else if (progress < 100)
                args.PushMarkup("It looks like the fuel tank is almost full.");
            else
                args.PushMarkup("It looks like the fuel tank is full.");
        }
    }

    private void OnLifeboatComputerUIOpenAttempt(Entity<LifeboatComputerComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled || ent.Comp.Enabled)
            return;

        args.Cancel();
        _popup.PopupClient("Evacuation has not been authorized.", ent, args.User, PopupType.SmallCaution);
    }

    private void OnEvacuationComputerLaunch(Entity<EvacuationComputerComponent> ent, ref EvacuationComputerLaunchBuiMsg args)
    {
        var user = args.Actor;
        if (ent.Comp.Mode != EvacuationComputerMode.Ready)
        {
            Log.Warning($"{ToPrettyString(user)} tried to activate evacuation computer {ToPrettyString(ent)} that is not ready. Mode: {ent.Comp.Mode}");
            return;
        }

        if (Transform(ent).GridUid is not { } gridId)
        {
            Log.Warning($"{ToPrettyString(user)} tried to activate evacuation computer {ToPrettyString(ent)} not on grid");
            return;
        }

        var gridTransform = Transform(gridId);
        if (ent.Comp.MaxMobs is { } maxMobs)
        {
            var mobs = 0;
            var children = gridTransform.ChildEnumerator;
            while (children.MoveNext(out var uid))
            {
                var mob = _mobStateQuery.HasComp(uid);
                if (!mob)
                {
                    if (TryComp(uid, out ContainerManagerComponent? containerManager))
                    {
                        foreach (var container in _container.GetAllContainers(uid, containerManager))
                        {
                            if (container.ContainedEntities.Any(_mobStateQuery.HasComp))
                            {
                                mob = true;
                                break;
                            }
                        }
                    }
                }

                if (mob)
                {
                    mobs++;

                    if (mobs > maxMobs && ent.Comp.Mode != EvacuationComputerMode.Crashed)
                    {
                        ent.Comp.Mode = EvacuationComputerMode.Crashed;
                        _popup.PopupClient("The evacuation pod is overloaded with this many people inside!", ent, user, PopupType.LargeCaution);

                        var time = _timing.CurTime;
                        var detonating = EnsureComp<DetonatingEvacuationComputerComponent>(ent);
                        detonating.DetonateAt = time + ent.Comp.DetonateDelay;
                        detonating.EjectAt = time + ent.Comp.EjectDelay;
                    }
                }

                if (_doorQuery.TryComp(uid, out var door))
                {
                    var evacuationDoor = EnsureComp<EvacuationDoorComponent>(uid);
                    evacuationDoor.Locked = true;
                    Dirty(uid, evacuationDoor);
                    _door.TryClose(uid, door);
                }
            }
        }

        _audio.PlayPredicted(ent.Comp.WarmupSound, ent, user);

        if (ent.Comp.Mode == EvacuationComputerMode.Crashed)
            return;

        ent.Comp.Mode = EvacuationComputerMode.Travelling;
        Dirty(ent);

        var crashChance = IsEvacuationComplete() ? 0 : ent.Comp.EarlyCrashChance;
        LaunchEvacuationFTL(gridId, crashChance, ent.Comp.LaunchSound);
    }

    private void OnLifeboatComputerLaunch(Entity<LifeboatComputerComponent> ent, ref LifeboatComputerLaunchBuiMsg args)
    {
        var user = args.Actor;
        if (!ent.Comp.Enabled)
        {
            Log.Warning($"{ToPrettyString(user)} tried to activate lifeboat computer {ToPrettyString(ent)} that is not ready.");
            return;
        }

        if (Transform(ent).GridUid is not { } gridId)
            return;

        ent.Comp.Enabled = false;
        Dirty(ent);

        var crashChance = IsEvacuationComplete() ? 0 : ent.Comp.EarlyCrashChance;
        LaunchEvacuationFTL(gridId, crashChance, null);
    }

    protected virtual void LaunchEvacuationFTL(EntityUid grid, float crashLandChance, SoundSpecifier? launchSound)
    {
    }

    private void SetPumpAppearance(EvacuationPumpVisuals visual)
    {
        var pumps = EntityQueryEnumerator<EvacuationPumpComponent>();
        while (pumps.MoveNext(out var uid, out _))
        {
            _appearance.SetData(uid, EvacuationPumpLayers.Layer, visual);
        }
    }

    private void SetPumpAmbience()
    {
        var pumps = EntityQueryEnumerator<EvacuationPumpComponent>();
        while (pumps.MoveNext(out var uid, out var pump))
        {
            _ambientSound.SetSound(uid, pump.ActiveSound);
        }
    }

    private IEnumerable<EntityUid> GetEvacuationAreas(EntityCoordinates coordinates)
    {
        if (!_area.TryGetAllAreas(coordinates, out var areaGrid))
            yield break;

        foreach (var areaId in areaGrid.Value.Comp.AreaEntities.Values)
        {
            if (!_areaQuery.TryComp(areaId, out var area) ||
                !area.HijackEvacuationArea)
            {
                continue;
            }

            yield return areaId;
        }
    }

    private bool IsAreaPumpPowered(EntityUid area)
    {
        return _rmcPower.IsAreaPowered(area, RMCPowerChannel.Equipment);
    }

    public void ToggleEvacuation(SoundSpecifier? startSound, SoundSpecifier? cancelSound, EntityUid? map)
    {
        DebugTools.Assert(map != null);

        var progress = EnsureComp<EvacuationProgressComponent>(map.Value);

        progress.Enabled = !progress.Enabled;
        Dirty(map.Value, progress);

        if (progress.Enabled)
        {
            _marineAnnounce.AnnounceARESStaging(
                null,
                "Attention. Emergency. All personnel must evacuate immediately.",
                startSound
            );
            var ev = new EvacuationEnabledEvent();
            RaiseLocalEvent(map.Value, ref ev, true);
        }
        else
        {
            _marineAnnounce.AnnounceARESStaging(null, "Evacuation has been cancelled.", cancelSound);
            var ev = new EvacuationDisabledEvent();
            RaiseLocalEvent(map.Value, ref ev, true);
        }
    }

    public bool IsEvacuationInProgress()
    {
        var query = EntityQueryEnumerator<EvacuationProgressComponent>();
        while (query.MoveNext(out _))
        {
            return true;
        }

        return false;
    }

    public bool IsEvacuationEnabled()
    {
        var query = EntityQueryEnumerator<EvacuationProgressComponent>();
        while (query.MoveNext(out var progress))
        {
            if (progress.Enabled)
                return true;
        }

        return false;
    }

    public int GetEvacuationProgress()
    {
        var query = EntityQueryEnumerator<EvacuationProgressComponent>();
        while (query.MoveNext(out var progress))
        {
            return (int) progress.Progress;
        }

        return 0;
    }

    public bool IsEvacuationComplete()
    {
        return GetEvacuationProgress() >= 100;
    }

    private void ProcessEvacuation()
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<EvacuationProgressComponent>();
        while (query.MoveNext(out var uid, out var progress))
        {
            //Only start fueling once the dropship has crashed into the Almayer
            if (!progress.DropShipCrashed)
                return;

            if (!progress.StartAnnounced)
            {
                progress.StartAnnounced = true;
                SetPumpAppearance(EvacuationPumpVisuals.Empty);
                SetPumpAmbience();

                var areas = new StringBuilder();
                foreach (var areaId in GetEvacuationAreas(uid.ToCoordinates()))
                {
                    var powered = IsAreaPumpPowered(areaId);
                    var line = $"[{Name(areaId)}] - [{(powered ? "Online" : "Offline")}]";
                    areas.AppendLine(line);
                }

                areas.Append(
                    "Due to low orbit, extra fuel is required for non-surface evacuations.\nMaintain fueling functionality for optimal evacuation conditions.");
                _marineAnnounce.AnnounceARESStaging(null, areas.ToString());
            }

            if (progress.NextUpdate > time)
                continue;

            progress.NextUpdate = time + progress.UpdateEvery;
            Dirty(uid, progress);

            double progressAdd = 0;
            double progressMultiply = 1;
            foreach (var areaId in GetEvacuationAreas(uid.ToCoordinates()))
            {
                if (!_areaQuery.TryComp(areaId, out var area) ||
                    !area.HijackEvacuationArea)
                {
                    continue;
                }

                var powered = IsAreaPumpPowered(areaId);
                if (progress.LastPower.TryGetValue(areaId, out var lastPower) &&
                    lastPower != powered)
                {
                    _marineAnnounce.AnnounceARESStaging(null, $"{Name(areaId)} - [{(powered ? "Online" : "Offline")}]");
                }

                progress.LastPower[areaId] = powered;
                if (!powered)
                    continue;

                switch (area.HijackEvacuationType)
                {
                    case AreaHijackEvacuationType.Add:
                        progressAdd += area.HijackEvacuationWeight;
                        break;
                    case AreaHijackEvacuationType.Multiply:
                        progressMultiply += area.HijackEvacuationWeight;
                        break;
                    default:
                        continue;
                }
            }

            progress.Progress = Math.Min(progress.Required, progress.Progress + progressAdd * progressMultiply);

            if (progress.Progress >= progress.NextAnnounce)
            {
                var current = progress.NextAnnounce;
                progress.NextAnnounce = current + progress.AnnounceEvery;

                var onAreas = string.Join(", ",
                    progress.LastPower.Where(kvp => kvp.Value).Select(kvp => Name(kvp.Key)));
                var offAreas = string.Join(", ",
                    progress.LastPower.Where(kvp => !kvp.Value).Select(kvp => Name(kvp.Key)));

                string MarinePercentageString(int percentage)
                {
                    var marineAnnounce = $"Emergency fuel replenishment is at {percentage} percent.";
                    if (offAreas.Length == 0)
                        marineAnnounce += " All fueling areas operational.";
                    else
                        marineAnnounce += $"To increase speed, restore power to the following areas: {offAreas}";

                    return marineAnnounce;
                }

                if (progress.Progress >= progress.Required)
                {
                    _marineAnnounce.AnnounceARESStaging(null, "Emergency fuel replenishment is at 100 percent. Safe utilization of lifeboats and pods is now possible.");
                    _xenoAnnounce.AnnounceAll(default, "The talls have completed their goals!");
                    SetPumpAppearance(EvacuationPumpVisuals.Full);
                    var ev = new EvacuationProgressEvent(100);
                    RaiseLocalEvent(uid, ref ev, true);
                }
                else if (progress.Progress >= progress.Required * 0.75)
                {
                    _marineAnnounce.AnnounceARESStaging(null, MarinePercentageString(75));

                    var xenoAnnounce = "The talls are three quarters of the way towards their goals.";
                    if (onAreas.Length > 0)
                        xenoAnnounce += $" Disable the following areas: {onAreas}";

                    _xenoAnnounce.AnnounceAll(default, xenoAnnounce);
                    SetPumpAppearance(EvacuationPumpVisuals.SeventyFive);

                    var ev = new EvacuationProgressEvent(75);
                    RaiseLocalEvent(uid, ref ev, true);
                }
                else if (progress.Progress >= progress.Required * 0.5)
                {
                    _marineAnnounce.AnnounceARESStaging(null, MarinePercentageString(50));

                    var xenoAnnounce = "The talls are half way towards their goals.";
                    if (onAreas.Length > 0)
                        xenoAnnounce += $" Disable the following areas: {onAreas}";

                    _xenoAnnounce.AnnounceAll(default, xenoAnnounce);
                    SetPumpAppearance(EvacuationPumpVisuals.Fifty);
                    var ev = new EvacuationProgressEvent(50);
                    RaiseLocalEvent(uid, ref ev, true);
                }
                else if (progress.Progress >= progress.Required * 0.25)
                {
                    var marineAnnounce = "Emergency fuel replenishment is at 25 percent. Lifeboat emergency early launch is now available.";
                    if (offAreas.Length == 0)
                        marineAnnounce += " All fueling areas operational.";
                    else
                        marineAnnounce += $" To increase speed, restore power to the following areas: {offAreas}";

                    _marineAnnounce.AnnounceARESStaging(null, marineAnnounce);

                    var xenoAnnounce = "The talls are a quarter of the way towards their goals.";
                    if (onAreas.Length > 0)
                        xenoAnnounce += $" Disable the following areas: {onAreas}";

                    _xenoAnnounce.AnnounceAll(default, xenoAnnounce);

                    SetPumpAppearance(EvacuationPumpVisuals.TwentyFive);
                    var ev = new EvacuationProgressEvent(25);
                    RaiseLocalEvent(uid, ref ev, true);
                }
            }
        }
    }

    private void ProcessExplodingPods()
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<DetonatingEvacuationComputerComponent>();
        while (query.MoveNext(out var uid, out var detonating))
        {
            if (Transform(uid).GridUid is not { } grid)
                continue;

            if (!TryComp(grid, out MapGridComponent? gridComp))
                continue;

            var gridTransform = Transform(grid);

            if (!detonating.Detonated && time >= detonating.DetonateAt)
            {
                detonating.Detonated = true;
                Dirty(uid, detonating);

                var coordinates = _transform.ToMapCoordinates(gridTransform.Coordinates);
                _rmcExplosion.QueueExplosion(coordinates, "RMC", 40, 5, 25, uid, canCreateVacuum: false);
            }

            if (!detonating.Ejected && time >= detonating.EjectAt)
            {
                detonating.Ejected = true;
                Dirty(uid, detonating);

                var children = gridTransform.ChildEnumerator;

                while (children.MoveNext(out var child))
                {
                    _hyperSleep.EjectChamber(child);

                    if (_doorQuery.TryComp(child, out var door))
                    {
                        var evacuationDoor = EnsureComp<EvacuationDoorComponent>(child);
                        evacuationDoor.Locked = false;
                        Dirty(child, evacuationDoor);
                        _door.TryOpenAndBolt(child, door);
                    }
                }
            }

            if (detonating.Detonated && detonating.Ejected)
                RemCompDeferred<DetonatingEvacuationComputerComponent>(uid);
        }
    }

    public override void Update(float frameTime)
    {
        ProcessEvacuation();
        ProcessExplodingPods();
    }
}
