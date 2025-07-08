using System.Linq;
using Content.Server._RMC14.Announce;
using Content.Server._RMC14.Marines;
using Content.Server.Administration.Logs;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.HiveLeader;
using Content.Shared.Actions;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.TacticalMap;

public sealed class TacticalMapSystem : SharedTacticalMapSystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly XenoEvolutionSystem _evolution = default!;
    [Dependency] private readonly MarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly XenoAnnounceSystem _xenoAnnounce = default!;

    private EntityQuery<ActiveTacticalMapTrackedComponent> _activeTacticalMapTrackedQuery;
    private EntityQuery<MarineMapTrackedComponent> _marineMapTrackedQuery;
    private EntityQuery<MapGridComponent> _mapGridQuery;
    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<RottingComponent> _rottingQuery;
    private EntityQuery<SquadTeamComponent> _squadTeamQuery;
    private EntityQuery<TacticalMapIconComponent> _tacticalMapIconQuery;
    private EntityQuery<TacticalMapComponent> _tacticalMapQuery;
    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<XenoMapTrackedComponent> _xenoMapTrackedQuery;
    private EntityQuery<XenoStructureMapTrackedComponent> _xenoStructureMapTrackedQuery;

    private readonly HashSet<Entity<TacticalMapTrackedComponent>> _toInit = new();
    private readonly HashSet<Entity<ActiveTacticalMapTrackedComponent>> _toUpdate = new();
    private readonly List<TacticalMapLine> _emptyLines = new();
    private TimeSpan _announceCooldown;
    private TimeSpan _mapUpdateEvery;

    public override void Initialize()
    {
        base.Initialize();

        _activeTacticalMapTrackedQuery = GetEntityQuery<ActiveTacticalMapTrackedComponent>();
        _marineMapTrackedQuery = GetEntityQuery<MarineMapTrackedComponent>();
        _mapGridQuery = GetEntityQuery<MapGridComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _rottingQuery = GetEntityQuery<RottingComponent>();
        _squadTeamQuery = GetEntityQuery<SquadTeamComponent>();
        _tacticalMapIconQuery = GetEntityQuery<TacticalMapIconComponent>();
        _tacticalMapQuery = GetEntityQuery<TacticalMapComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();
        _xenoMapTrackedQuery = GetEntityQuery<XenoMapTrackedComponent>();
        _xenoStructureMapTrackedQuery = GetEntityQuery<XenoStructureMapTrackedComponent>();

        SubscribeLocalEvent<XenoOvipositorChangedEvent>(OnOvipositorChanged);

        SubscribeLocalEvent<TacticalMapComponent, MapInitEvent>(OnTacticalMapMapInit);

        SubscribeLocalEvent<TacticalMapUserComponent, MapInitEvent>(OnUserMapInit);
        SubscribeLocalEvent<TacticalMapUserComponent, OpenTacMapAlertEvent>(OnUserOpenAlert);

        SubscribeLocalEvent<TacticalMapComputerComponent, MapInitEvent>(OnComputerMapInit);
        SubscribeLocalEvent<TacticalMapComputerComponent, BeforeActivatableUIOpenEvent>(OnComputerBeforeUIOpen);

        SubscribeLocalEvent<TacticalMapTrackedComponent, MapInitEvent>(OnTrackedMapInit);
        SubscribeLocalEvent<TacticalMapTrackedComponent, MobStateChangedEvent>(OnTrackedMobStateChanged);
        SubscribeLocalEvent<TacticalMapTrackedComponent, RoleAddedEvent>(OnTrackedChanged);
        SubscribeLocalEvent<TacticalMapTrackedComponent, MindAddedMessage>(OnTrackedChanged);
        SubscribeLocalEvent<TacticalMapTrackedComponent, SquadMemberUpdatedEvent>(OnTrackedChanged);
        SubscribeLocalEvent<TacticalMapTrackedComponent, EntParentChangedMessage>(OnTrackedChanged);

        SubscribeLocalEvent<ActiveTacticalMapTrackedComponent, ComponentRemove>(OnActiveRemove);
        SubscribeLocalEvent<ActiveTacticalMapTrackedComponent, EntityTerminatingEvent>(OnActiveRemove);
        SubscribeLocalEvent<ActiveTacticalMapTrackedComponent, MoveEvent>(OnActiveTrackedMove);
        SubscribeLocalEvent<ActiveTacticalMapTrackedComponent, RoleAddedEvent>(OnActiveTrackedRoleAdded);
        SubscribeLocalEvent<ActiveTacticalMapTrackedComponent, MindAddedMessage>(OnActiveTrackedMindAdded);
        SubscribeLocalEvent<ActiveTacticalMapTrackedComponent, SquadMemberUpdatedEvent>(OnActiveSquadMemberUpdated);
        SubscribeLocalEvent<ActiveTacticalMapTrackedComponent, MobStateChangedEvent>(OnActiveMobStateChanged);
        SubscribeLocalEvent<ActiveTacticalMapTrackedComponent, HiveLeaderStatusChangedEvent>(OnHiveLeaderStatusChanged);

        SubscribeLocalEvent<MapBlipIconOverrideComponent, MapInitEvent>(OnMapBlipOverrideMapInit);

        SubscribeLocalEvent<RottingComponent, MapInitEvent>(OnRottingMapInit);
        SubscribeLocalEvent<RottingComponent, ComponentRemove>(OnRottingRemove);

        SubscribeLocalEvent<TacticalMapLiveUpdateOnOviComponent, MapInitEvent>(OnLiveUpdateOnOviMapInit);
        SubscribeLocalEvent<TacticalMapLiveUpdateOnOviComponent, MobStateChangedEvent>(OnLiveUpdateOnOviStateChanged);

        Subs.BuiEvents<TacticalMapUserComponent>(TacticalMapUserUi.Key,
            subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnUserBUIOpened);
                subs.Event<BoundUIClosedEvent>(OnUserBUIClosed);
                subs.Event<TacticalMapUpdateCanvasMsg>(OnUserUpdateCanvasMsg);
            });

        Subs.BuiEvents<TacticalMapComputerComponent>(TacticalMapComputerUi.Key,
            subs =>
            {
                subs.Event<TacticalMapUpdateCanvasMsg>(OnComputerUpdateCanvasMsg);
            });

        Subs.CVar(_config,
            RMCCVars.RMCTacticalMapAnnounceCooldownSeconds,
            v => _announceCooldown = TimeSpan.FromSeconds(v),
            true);

        Subs.CVar(_config,
            RMCCVars.RMCTacticalMapUpdateEverySeconds,
            v => _mapUpdateEvery = TimeSpan.FromSeconds(v),
            true);
    }

    private void OnOvipositorChanged(ref XenoOvipositorChangedEvent ev)
    {
        var users = EntityQueryEnumerator<TacticalMapLiveUpdateOnOviComponent, TacticalMapUserComponent>();
        while (users.MoveNext(out var uid, out var onOvi, out var user))
        {
            if (!onOvi.Enabled)
                continue;

            user.LiveUpdate = ev.Attached;
            Dirty(uid, user);
        }
    }

    private void OnTacticalMapMapInit(Entity<TacticalMapComponent> ent, ref MapInitEvent args)
    {
        var tracked = EntityQueryEnumerator<ActiveTacticalMapTrackedComponent, TacticalMapTrackedComponent>();
        while (tracked.MoveNext(out var uid, out var active, out var comp))
        {
            UpdateActiveTracking((uid, comp));
            UpdateTracked((uid, active));
        }

        var users = EntityQueryEnumerator<TacticalMapUserComponent>();
        while (users.MoveNext(out var userId, out var userComp))
        {
            userComp.Map = ent;
            Dirty(userId, userComp);
        }

        var computers = EntityQueryEnumerator<TacticalMapComputerComponent>();
        while (computers.MoveNext(out var computerId, out var computerComp))
        {
            computerComp.Map = ent;
            Dirty(computerId, computerComp);
        }
    }

    private void OnUserMapInit(Entity<TacticalMapUserComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.Action, ent.Comp.ActionId);

        if (TryGetTacticalMap(out var map))
            ent.Comp.Map = map;

        Dirty(ent);
    }

    protected override void OnUserOpenAction(Entity<TacticalMapUserComponent> ent, ref OpenTacticalMapActionEvent args)
    {
        base.OnUserOpenAction(ent, ref args);

        if (TryGetTacticalMap(out var map))
            UpdateUserData(ent, map);
    }

    private void OnUserOpenAlert(Entity<TacticalMapUserComponent> ent, ref OpenTacMapAlertEvent args)
    {
        if (TryGetTacticalMap(out var map))
            UpdateUserData(ent, map);

        _ui.TryOpenUi(ent.Owner, TacticalMapUserUi.Key, ent);
    }

    private void OnComputerMapInit(Entity<TacticalMapComputerComponent> ent, ref MapInitEvent args)
    {
        if (TryGetTacticalMap(out var map))
            ent.Comp.Map = map;

        Dirty(ent);
    }

    private void OnComputerBeforeUIOpen(Entity<TacticalMapComputerComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateMapData((ent, ent));
    }

    private void OnTrackedMapInit(Entity<TacticalMapTrackedComponent> ent, ref MapInitEvent args)
    {
        _toInit.Add(ent);
        if (TryComp(ent, out ActiveTacticalMapTrackedComponent? active))
            _toUpdate.Add((ent, active));
    }

    private void OnTrackedMobStateChanged(Entity<TacticalMapTrackedComponent> ent, ref MobStateChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        UpdateActiveTracking(ent, args.NewMobState);
    }

    private void OnTrackedChanged<T>(Entity<TacticalMapTrackedComponent> ent, ref T args)
    {
        if (_timing.ApplyingState || TerminatingOrDeleted(ent))
            return;

        UpdateActiveTracking(ent);
    }

    private void OnActiveRemove<T>(Entity<ActiveTacticalMapTrackedComponent> ent, ref T args)
    {
        BreakTracking(ent);
    }

    private void OnActiveTrackedMove(Entity<ActiveTacticalMapTrackedComponent> ent, ref MoveEvent args)
    {
        _toUpdate.Add(ent);
    }

    private void OnActiveTrackedRoleAdded(Entity<ActiveTacticalMapTrackedComponent> ent, ref RoleAddedEvent args)
    {
        UpdateIcon(ent);
        UpdateTracked(ent);
    }

    private void OnActiveTrackedMindAdded(Entity<ActiveTacticalMapTrackedComponent> ent, ref MindAddedMessage args)
    {
        UpdateIcon(ent);
        UpdateTracked(ent);
    }

    private void OnActiveSquadMemberUpdated(Entity<ActiveTacticalMapTrackedComponent> ent, ref SquadMemberUpdatedEvent args)
    {
        if (_squadTeamQuery.TryComp(args.Squad, out var squad))
        {
            if (squad.MinimapBackground != null)
            {
                ent.Comp.Background = squad.MinimapBackground;
                ent.Comp.Color = Color.White;
            }
            else
                ent.Comp.Color = squad.Color;
        }
        else if (ent.Comp.Background != null) // If we lose a squad update icon to refresh background if needed
            UpdateIcon(ent);
    }

    private void OnActiveMobStateChanged(Entity<ActiveTacticalMapTrackedComponent> ent, ref MobStateChangedEvent args)
    {
        UpdateIcon(ent);
        UpdateTracked(ent);
    }

    private void OnHiveLeaderStatusChanged(Entity<ActiveTacticalMapTrackedComponent> ent, ref HiveLeaderStatusChangedEvent args)
    {
        UpdateIcon(ent);
        UpdateHiveLeader(ent, args.BecameLeader);
        UpdateTracked(ent);
    }

    private void OnMapBlipOverrideMapInit(Entity<MapBlipIconOverrideComponent> ent, ref MapInitEvent args)
    {
        if (_activeTacticalMapTrackedQuery.TryComp(ent, out var active))
        {
            UpdateIcon((ent.Owner, active));
            UpdateTracked((ent.Owner, active));
        }
    }

    private void OnRottingMapInit(Entity<RottingComponent> ent, ref MapInitEvent args)
    {
        if (_activeTacticalMapTrackedQuery.TryComp(ent, out var active))
            UpdateTracked((ent, active));
    }

    private void OnRottingRemove(Entity<RottingComponent> ent, ref ComponentRemove args)
    {
        if (_activeTacticalMapTrackedQuery.TryComp(ent, out var active))
            UpdateTracked((ent, active));
    }

    private void OnLiveUpdateOnOviMapInit(Entity<TacticalMapLiveUpdateOnOviComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.Enabled ||
            !TryComp(ent, out TacticalMapUserComponent? user))
        {
            return;
        }

        user.LiveUpdate = _evolution.HasOvipositor();
        Dirty(ent, user);
    }

    private void OnLiveUpdateOnOviStateChanged(Entity<TacticalMapLiveUpdateOnOviComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            RemCompDeferred<TacticalMapLiveUpdateOnOviComponent>(ent);
    }

    private void OnUserBUIOpened(Entity<TacticalMapUserComponent> ent, ref BoundUIOpenedEvent args)
    {
        EnsureComp<ActiveTacticalMapUserComponent>(ent);
    }

    private void OnUserBUIClosed(Entity<TacticalMapUserComponent> ent, ref BoundUIClosedEvent args)
    {
        RemCompDeferred<ActiveTacticalMapUserComponent>(ent);
    }

    private void OnUserUpdateCanvasMsg(Entity<TacticalMapUserComponent> ent, ref TacticalMapUpdateCanvasMsg args)
    {
        var user = args.Actor;
        if (!ent.Comp.CanDraw)
            return;

        var lines = args.Lines;
        if (lines.Count > LineLimit)
            lines = lines[..LineLimit];

        var time = _timing.CurTime;
        if (time < ent.Comp.NextAnnounceAt)
            return;

        var nextAnnounce = time + _announceCooldown;
        ent.Comp.LastAnnounceAt = time;
        ent.Comp.NextAnnounceAt = nextAnnounce;
        Dirty(ent);

        if (ent.Comp.Marines)
            UpdateCanvas(lines, true, false, user, ent.Comp.Sound);

        if (ent.Comp.Xenos)
            UpdateCanvas(lines, false, true, user, ent.Comp.Sound);
    }

    private void OnComputerUpdateCanvasMsg(Entity<TacticalMapComputerComponent> ent, ref TacticalMapUpdateCanvasMsg args)
    {
        var user = args.Actor;
        if (!_skills.HasSkill(user, ent.Comp.Skill, ent.Comp.SkillLevel))
            return;

        var lines = args.Lines;
        if (lines.Count > LineLimit)
            lines = lines[..LineLimit];

        var time = _timing.CurTime;
        if (time < ent.Comp.NextAnnounceAt)
            return;

        var nextAnnounce = time + _announceCooldown;
        ent.Comp.NextAnnounceAt = nextAnnounce;
        Dirty(ent);

        var computers = EntityQueryEnumerator<TacticalMapComputerComponent>();
        while (computers.MoveNext(out var uid, out var computer))
        {
            computer.LastAnnounceAt = time;
            computer.NextAnnounceAt = nextAnnounce;
            Dirty(uid, computer);
        }

        UpdateCanvas(lines, true, false, user);
    }

    private void UpdateActiveTracking(Entity<TacticalMapTrackedComponent> tracked, MobState mobState)
    {
        if (!tracked.Comp.TrackDead && mobState == MobState.Dead)
        {
            RemCompDeferred<ActiveTacticalMapTrackedComponent>(tracked);
            return;
        }

        if (LifeStage(tracked) < EntityLifeStage.MapInitialized)
            return;

        var active = EnsureComp<ActiveTacticalMapTrackedComponent>(tracked);
        var activeEnt = new Entity<ActiveTacticalMapTrackedComponent>(tracked, active);
        UpdateIcon(activeEnt);
        UpdateRotting(activeEnt);
        UpdateColor(activeEnt);
    }

    private void UpdateActiveTracking(Entity<TacticalMapTrackedComponent> tracked)
    {
        var state = _mobStateQuery.CompOrNull(tracked)?.CurrentState ?? MobState.Alive;
        UpdateActiveTracking(tracked, state);
    }

    private void BreakTracking(Entity<ActiveTacticalMapTrackedComponent> tracked)
    {
        if (!_tacticalMapQuery.TryComp(tracked.Comp.Map, out var tacticalMap))
            return;

        tacticalMap.MarineBlips.Remove(tracked.Owner.Id);
        tacticalMap.XenoBlips.Remove(tracked.Owner.Id);
        tacticalMap.XenoStructureBlips.Remove(tracked.Owner.Id);
        tacticalMap.MapDirty = true;
        tracked.Comp.Map = null;
    }

    private void UpdateIcon(Entity<ActiveTacticalMapTrackedComponent> tracked)
    {
        SpriteSpecifier.Rsi? mapBlipOverride = null;
        if (TryComp<MapBlipIconOverrideComponent>(tracked, out var mapBlipOverrideComp) && mapBlipOverrideComp.Icon != null)
            mapBlipOverride = mapBlipOverrideComp.Icon;

        if (_tacticalMapIconQuery.TryComp(tracked, out var iconComp))
        {
            tracked.Comp.Icon = mapBlipOverride ?? iconComp.Icon;
            tracked.Comp.Background = iconComp.Background;
            UpdateSquadBackground(tracked);
            return;
        }

        tracked.Comp.Icon = mapBlipOverride;
        UpdateSquadBackground(tracked);
    }

    private void UpdateSquadBackground(Entity<ActiveTacticalMapTrackedComponent> tracked)
    {
        //Don't get job background if we have a squad, and if we do and it doesn't have it's own background
        //Still don't apply it
        if (!_squad.TryGetMemberSquad(tracked.Owner, out var squad))
            return;

        tracked.Comp.Background = squad.Comp.MinimapBackground;
        if (TryComp(tracked, out TacticalMapIconComponent? icon))
        {
            icon.Background = tracked.Comp.Background;
            Dirty(tracked, icon);
        }
    }

    private void UpdateRotting(Entity<ActiveTacticalMapTrackedComponent> tracked)
    {
        tracked.Comp.Undefibbable = _rottingQuery.HasComp(tracked);
    }

    private void UpdateColor(Entity<ActiveTacticalMapTrackedComponent> tracked)
    {
        if (_squad.TryGetMemberSquad(tracked.Owner, out var squad))
        {
            if (squad.Comp.MinimapBackground == null)
                tracked.Comp.Color = squad.Comp.Color;
            else
            {
                tracked.Comp.Background = squad.Comp.MinimapBackground;
                tracked.Comp.Color = Color.White;
            }
        }
        else
        {
            tracked.Comp.Color = Color.White;
        }

        if (TryComp(tracked, out TacticalMapIconComponent? icon))
        {
            icon.Background = tracked.Comp.Background;
            Dirty(tracked, icon);
        }
    }

    private void UpdateHiveLeader(Entity<ActiveTacticalMapTrackedComponent> tracked, bool isLeader)
    {
        tracked.Comp.HiveLeader = isLeader;
    }

    private void UpdateTracked(Entity<ActiveTacticalMapTrackedComponent> ent)
    {
        if (!_transformQuery.TryComp(ent.Owner, out var xform) ||
            xform.GridUid is not { } gridId ||
            !_mapGridQuery.TryComp(gridId, out var gridComp) ||
            !_tacticalMapQuery.TryComp(gridId, out var tacticalMap) ||
            !_transform.TryGetGridTilePosition((ent.Owner, xform), out var indices, gridComp))
        {
            BreakTracking(ent);
            return;
        }

        if (ent.Comp.Icon == null)
            UpdateIcon(ent);

        if (ent.Comp.Icon is not { } icon)
        {
            BreakTracking(ent);
            return;
        }

        if (ent.Comp.Map != xform.GridUid)
        {
            BreakTracking(ent);
            ent.Comp.Map = xform.GridUid;
        }

        var status = TacticalMapBlipStatus.Alive;
        if (_rottingQuery.HasComp(ent))
            status = TacticalMapBlipStatus.Undefibabble;
        else if (_mobState.IsDead(ent))
            status = TacticalMapBlipStatus.Defibabble;

        var blip = new TacticalMapBlip(indices, icon, ent.Comp.Color, status, ent.Comp.Background, ent.Comp.HiveLeader);
        if (_marineMapTrackedQuery.HasComp(ent))
        {
            tacticalMap.MarineBlips[ent.Owner.Id] = blip;
            tacticalMap.MapDirty = true;
        }

        if (_xenoMapTrackedQuery.HasComp(ent))
        {
            tacticalMap.XenoBlips[ent.Owner.Id] = blip;
            tacticalMap.MapDirty = true;
        }

        if (_xenoStructureMapTrackedQuery.HasComp(ent))
        {
            tacticalMap.XenoStructureBlips[ent.Owner.Id] = blip;
            tacticalMap.MapDirty = true;
        }
    }

    public override void UpdateUserData(Entity<TacticalMapUserComponent> user, TacticalMapComponent map)
    {
        var lines = EnsureComp<TacticalMapLinesComponent>(user);
        if (user.Comp.Xenos)
        {
            user.Comp.XenoBlips = user.Comp.LiveUpdate ? map.XenoBlips : map.LastUpdateXenoBlips;
            user.Comp.XenoStructureBlips = user.Comp.LiveUpdate ? map.XenoStructureBlips : map.LastUpdateXenoStructureBlips;

            var alwaysVisible = EntityQueryEnumerator<TacticalMapAlwaysVisibleComponent>();
            while (alwaysVisible.MoveNext(out var uid, out var comp))
            {
                if (!comp.VisibleToXenos)
                    continue;

                if (user.Comp.XenoBlips.ContainsKey(uid.Id) || user.Comp.XenoStructureBlips.ContainsKey(uid.Id))
                    continue;

                var blip = FindBlipInMap(uid.Id, map);
                if (blip == null)
                    continue;

                if (comp.VisibleAsXenoStructure)
                    user.Comp.XenoStructureBlips[uid.Id] = blip.Value;
                else
                    user.Comp.XenoBlips[uid.Id] = blip.Value;
            }

            lines.XenoLines = map.XenoLines;
            lines.MarineLines = _emptyLines;
        }

        if (user.Comp.Marines)
        {
            user.Comp.MarineBlips = user.Comp.LiveUpdate ? map.MarineBlips : map.LastUpdateMarineBlips;

            var alwaysVisible = EntityQueryEnumerator<TacticalMapAlwaysVisibleComponent>();
            while (alwaysVisible.MoveNext(out var uid, out var comp))
            {
                if (!comp.VisibleToMarines)
                    continue;

                if (user.Comp.MarineBlips.ContainsKey(uid.Id))
                    continue;

                var blip = FindBlipInMap(uid.Id, map);
                if (blip != null)
                    user.Comp.MarineBlips[uid.Id] = blip.Value;
            }

            lines.MarineLines = map.MarineLines;
            lines.XenoLines = _emptyLines;
        }

        Dirty(user);
        Dirty(user, lines);
    }

    private TacticalMapBlip? FindBlipInMap(int entityId, TacticalMapComponent map)
    {
        if (map.MarineBlips.TryGetValue(entityId, out var marineBlip))
            return marineBlip;
        if (map.XenoStructureBlips.TryGetValue(entityId, out var structureBlip))
            return structureBlip;
        if (map.XenoBlips.TryGetValue(entityId, out var xenoBlip))
            return xenoBlip;

        return null;
    }

    private void UpdateCanvas(List<TacticalMapLine> lines, bool marine, bool xeno, EntityUid user, SoundSpecifier? sound = null)
    {
        var maps = EntityQueryEnumerator<TacticalMapComponent>();
        while (maps.MoveNext(out var mapId, out var map))
        {
            map.MapDirty = true;

            if (marine)
            {
                map.MarineLines = lines;
                map.LastUpdateMarineBlips = map.MarineBlips.ToDictionary();

                var includeEv = new TacticalMapIncludeXenosEvent();
                RaiseLocalEvent(ref includeEv);
                if (includeEv.Include)
                {
                    foreach (var blip in map.XenoBlips)
                    {
                        map.LastUpdateMarineBlips.Add(blip.Key, blip.Value);
                    }
                }

                _marineAnnounce.AnnounceARES(user, "The UNMC tactical map has been updated.", sound);
                _adminLog.Add(LogType.RMCTacticalMapUpdated, $"{ToPrettyString(user)} updated the marine tactical map for {{ToPrettyString(mapId)}}");
            }

            if (xeno)
            {
                map.XenoLines = lines;
                map.LastUpdateXenoBlips = map.XenoBlips.ToDictionary();
                map.LastUpdateXenoStructureBlips = map.XenoStructureBlips.ToDictionary();
                _xenoAnnounce.AnnounceSameHive(user, "The Xenonid tactical map has been updated.", sound);
                _adminLog.Add(LogType.RMCTacticalMapUpdated, $"{ToPrettyString(user)} updated the xenonid tactical map for {ToPrettyString(mapId)}");
            }

            var ev = new TacticalMapUpdatedEvent(lines.ToList(), user);
            RaiseLocalEvent(ref ev);
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
        {
            _toInit.Clear();
            _toUpdate.Clear();
        }

        try
        {
            foreach (var init in _toInit)
            {
                if (!init.Comp.Running)
                    continue;

                var wasActive = HasComp<ActiveTacticalMapTrackedComponent>(init);
                UpdateActiveTracking(init);

                if (!wasActive && TryComp(init, out ActiveTacticalMapTrackedComponent? active))
                    UpdateTracked((init, active));
            }
        }
        finally
        {
            _toInit.Clear();
        }

        try
        {
            foreach (var update in _toUpdate)
            {
                if (!update.Comp.Running)
                    continue;

                UpdateTracked(update);
            }
        }
        finally
        {
            _toUpdate.Clear();
        }

        var time = _timing.CurTime;
        var maps = EntityQueryEnumerator<TacticalMapComponent>();
        while (maps.MoveNext(out var map))
        {
            if (!map.MapDirty || time < map.NextUpdate)
                continue;

            map.MapDirty = false;
            map.NextUpdate = time + _mapUpdateEvery;

            var computers = EntityQueryEnumerator<TacticalMapComputerComponent>();
            while (computers.MoveNext(out var computerId, out var computer))
            {
                if (!_ui.IsUiOpen(computerId, TacticalMapComputerUi.Key))
                    continue;

                UpdateMapData((computerId, computer), map);
            }

            var users = EntityQueryEnumerator<ActiveTacticalMapUserComponent, TacticalMapUserComponent>();
            while (users.MoveNext(out var userId, out _, out var userComp))
            {
                UpdateUserData((userId, userComp), map);
            }
        }
    }
}
