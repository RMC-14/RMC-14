using System;
using System.Linq;
using System.Numerics;
using Content.Server._RMC14.Announce;
using Content.Server._RMC14.Marines;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking.Events;
using Content.Server._RMC14.Xenonids.Watch;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Medical.Unrevivable;
using Content.Server._RMC14.Overwatch;
using Content.Shared._RMC14.Overwatch;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared.Popups;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Eye;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Survivor;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.HiveLeader;
using Content.Shared.Ghost;
using Content.Shared.Actions;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Content.Shared.Traits.Assorted;
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.TacticalMap;

public sealed class TacticalMapSystem : SharedTacticalMapSystem
{
    private static readonly ProtoId<TacticalMapLayerPrototype> GlobalMarineLayer = "Marines";

    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly XenoEvolutionSystem _evolution = default!;
    [Dependency] private readonly TacticalMapLayerAccessSystem _layerAccess = default!;
    [Dependency] private readonly MarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly OverwatchConsoleSystem _overwatch = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly TacticalMapReplaySystem _replay = default!;
    [Dependency] private readonly XenoAnnounceSystem _xenoAnnounce = default!;
    [Dependency] private readonly XenoWatchSystem _xenoWatch = default!;
    [Dependency] private readonly RMCUnrevivableSystem _unrevivableSystem = default!;

    private EntityQuery<ActiveTacticalMapTrackedComponent> _activeTacticalMapTrackedQuery;
    private EntityQuery<TacticalMapLayerTrackedComponent> _tacticalMapLayerTrackedQuery;
    private EntityQuery<MapGridComponent> _mapGridQuery;
    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<RottingComponent> _rottingQuery;
    private EntityQuery<SquadTeamComponent> _squadTeamQuery;
    private EntityQuery<TacticalMapIconComponent> _tacticalMapIconQuery;
    private EntityQuery<TacticalMapComponent> _tacticalMapQuery;
    private EntityQuery<TransformComponent> _transformQuery;

    private readonly HashSet<Entity<TacticalMapTrackedComponent>> _toInit = new();
    private readonly HashSet<Entity<ActiveTacticalMapTrackedComponent>> _toUpdate = new();
    private TimeSpan _announceCooldown;
    private TimeSpan _mapUpdateEvery;
    private TimeSpan _forceMapUpdateEvery;
    private TimeSpan _nextForceMapUpdate = TimeSpan.FromSeconds(30);

    public override void Initialize()
    {
        base.Initialize();

        _activeTacticalMapTrackedQuery = GetEntityQuery<ActiveTacticalMapTrackedComponent>();
        _tacticalMapLayerTrackedQuery = GetEntityQuery<TacticalMapLayerTrackedComponent>();
        _mapGridQuery = GetEntityQuery<MapGridComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _rottingQuery = GetEntityQuery<RottingComponent>();
        _squadTeamQuery = GetEntityQuery<SquadTeamComponent>();
        _tacticalMapIconQuery = GetEntityQuery<TacticalMapIconComponent>();
        _tacticalMapQuery = GetEntityQuery<TacticalMapComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<XenoOvipositorChangedEvent>(OnOvipositorChanged);

        SubscribeLocalEvent<TacticalMapComponent, MapInitEvent>(OnTacticalMapMapInit);

        SubscribeLocalEvent<TacticalMapUserComponent, MapInitEvent>(OnUserMapInit);

        SubscribeLocalEvent<TacticalMapComputerComponent, MapInitEvent>(OnComputerMapInit);
        SubscribeLocalEvent<TacticalMapComputerComponent, BeforeActivatableUIOpenEvent>(OnComputerBeforeUIOpen);

        SubscribeLocalEvent<TacticalMapTrackedComponent, MapInitEvent>(OnTrackedMapInit);
        SubscribeLocalEvent<TacticalMapTrackedComponent, MobStateChangedEvent>(OnTrackedMobStateChanged);
        SubscribeLocalEvent<TacticalMapTrackedComponent, RoleAddedEvent>(OnTrackedChanged);
        SubscribeLocalEvent<TacticalMapTrackedComponent, MindAddedMessage>(OnTrackedChanged);
        SubscribeLocalEvent<TacticalMapTrackedComponent, SquadMemberUpdatedEvent>(OnTrackedChanged);
        SubscribeLocalEvent<TacticalMapTrackedComponent, EntParentChangedMessage>(OnTrackedChanged);
        SubscribeLocalEvent<SquadObjectivesChangedEvent>(OnSquadObjectivesChanged);
        SubscribeLocalEvent<TacticalMapLayerTrackedComponent, SquadMemberUpdatedEvent>(OnLayerTrackedSquadMemberUpdated);

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

        SubscribeLocalEvent<UnrevivableComponent, MapInitEvent>(OnUnrevivableMapInit);
        SubscribeLocalEvent<UnrevivableComponent, ComponentRemove>(OnUnrevivablRemove);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);

        SubscribeLocalEvent<TacticalMapLiveUpdateOnOviComponent, MapInitEvent>(OnLiveUpdateOnOviMapInit);
        SubscribeLocalEvent<TacticalMapLiveUpdateOnOviComponent, MobStateChangedEvent>(OnLiveUpdateOnOviStateChanged);

        Subs.BuiEvents<TacticalMapUserComponent>(TacticalMapUserUi.Key,
            subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnUserBUIOpened);
                subs.Event<BoundUIClosedEvent>(OnUserBUIClosed);
                subs.Event<TacticalMapSelectMapMsg>(OnUserSelectMapMsg);
                subs.Event<TacticalMapSelectLayerMsg>(OnUserSelectLayerMsg);
                subs.Event<TacticalMapUpdateCanvasMsg>(OnUserUpdateCanvasMsg);
                subs.Event<TacticalMapQueenEyeMoveMsg>(OnUserQueenEyeMoveMsg);
                subs.Event<TacticalMapXenoWatchBlipMsg>(OnUserXenoWatchBlipMsg);
                subs.Event<TacticalMapSetVisibleLayersMsg>(OnUserSetVisibleLayersMsg);
            });

        Subs.BuiEvents<TacticalMapComputerComponent>(TacticalMapComputerUi.Key,
            subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnComputerBUIOpened);
                subs.Event<TacticalMapSelectMapMsg>(OnComputerSelectMapMsg);
                subs.Event<TacticalMapSelectLayerMsg>(OnComputerSelectLayerMsg);
                subs.Event<TacticalMapUpdateCanvasMsg>(OnComputerUpdateCanvasMsg);
                subs.Event<TacticalMapOverwatchBlipMsg>(OnOverwatchBlipMsg);
                subs.Event<TacticalMapSetVisibleLayersMsg>(OnComputerSetVisibleLayersMsg);
            });

        Subs.CVar(_config,
            RMCCVars.RMCTacticalMapAnnounceCooldownSeconds,
            v => _announceCooldown = TimeSpan.FromSeconds(v),
            true);

        Subs.CVar(_config,
            RMCCVars.RMCTacticalMapUpdateEverySeconds,
            v => _mapUpdateEvery = TimeSpan.FromSeconds(v),
            true);

        Subs.CVar(_config,
            RMCCVars.RMCTacticalMapForceUpdateEverySeconds,
            v => _forceMapUpdateEvery = TimeSpan.FromSeconds(v),
            true);
    }

    protected override bool TryResolveUserMap(Entity<TacticalMapUserComponent> user, out Entity<TacticalMapComponent> map)
    {
        if (!TryResolveMap(user.Owner, user.Comp.Map, out map))
            return false;

        if (user.Comp.Map != map.Owner)
        {
            user.Comp.Map = map.Owner;
            Dirty(user);
        }

        return true;
    }

    protected override bool TryResolveComputerMap(Entity<TacticalMapComputerComponent> computer, out Entity<TacticalMapComponent> map)
    {
        if (!TryResolveMap(computer.Owner, computer.Comp.Map, out map))
            return false;

        if (computer.Comp.Map != map.Owner)
        {
            computer.Comp.Map = map.Owner;
            Dirty(computer);
        }

        return true;
    }

    private bool TryResolveMap(EntityUid owner, EntityUid? selectedMap, out Entity<TacticalMapComponent> map)
    {
        if (selectedMap != null && _tacticalMapQuery.TryComp(selectedMap.Value, out var selectedMapComp))
        {
            map = (selectedMap.Value, selectedMapComp);
            return true;
        }

        if (_transformQuery.TryComp(owner, out var xform))
        {
            if (xform.GridUid != null && _tacticalMapQuery.TryComp(xform.GridUid.Value, out var gridMap))
            {
                map = (xform.GridUid.Value, gridMap);
                return true;
            }

            if (xform.MapUid is { } mapUid && _tacticalMapQuery.TryComp(mapUid, out var mapComp))
            {
                map = (mapUid, mapComp);
                return true;
            }
        }

        return TryGetTacticalMap(out map);
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
            if (userComp.Map == null || !_tacticalMapQuery.HasComp(userComp.Map.Value))
            {
                userComp.Map = ent;
                Dirty(userId, userComp);
            }
        }

        var computers = EntityQueryEnumerator<TacticalMapComputerComponent>();
        while (computers.MoveNext(out var computerId, out var computerComp))
        {
            if (computerComp.Map == null || !_tacticalMapQuery.HasComp(computerComp.Map.Value))
            {
                computerComp.Map = ent;
                Dirty(computerId, computerComp);
            }
        }

        var activeUsers = EntityQueryEnumerator<ActiveTacticalMapUserComponent, TacticalMapUserComponent>();
        while (activeUsers.MoveNext(out var userId, out _, out var userComp))
        {
            UpdateTacticalMapState((userId, userComp));
        }

        var activeComputers = EntityQueryEnumerator<TacticalMapComputerComponent>();
        while (activeComputers.MoveNext(out var computerId, out var computerComp))
        {
            if (_ui.IsUiOpen(computerId, TacticalMapComputerUi.Key))
                UpdateTacticalMapComputerState((computerId, computerComp));
        }
    }

    private void OnUserMapInit(Entity<TacticalMapUserComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.Action, ent.Comp.ActionId);

        TryResolveUserMap(ent, out _);
        RefreshUserVisibleLayers(ent);

        Dirty(ent);
    }

    private void OnComputerMapInit(Entity<TacticalMapComputerComponent> ent, ref MapInitEvent args)
    {
        TryResolveComputerMap(ent, out _);
        RefreshComputerVisibleLayers(ent);

        Dirty(ent);
    }

    private void OnComputerBeforeUIOpen(Entity<TacticalMapComputerComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        if (TryResolveComputerMap(ent, out var map))
            UpdateMapData((ent, ent), map.Comp);
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
        else if (ent.Comp.Background != null)
            UpdateIcon(ent);
    }

    private void OnLayerTrackedSquadMemberUpdated(Entity<TacticalMapLayerTrackedComponent> ent, ref SquadMemberUpdatedEvent args)
    {
        if (!_squadTeamQuery.TryComp(args.Squad, out var squad) || squad.TacticalMapLayer == null)
            return;

        var changed = false;
        var allSquadLayers = GetAllSquadLayers();
        foreach (var layer in allSquadLayers)
        {
            if (ent.Comp.Layers.Remove(layer))
                changed = true;
        }

        var squadLayer = squad.TacticalMapLayer.Value;
        if (!ent.Comp.Layers.Contains(squadLayer))
        {
            ent.Comp.Layers.Add(squadLayer);
            changed = true;
        }

        if (changed)
        {
            Dirty(ent);
            if (_activeTacticalMapTrackedQuery.TryComp(ent.Owner, out var active))
                UpdateTracked((ent.Owner, active));
        }
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

    private void OnUnrevivableMapInit(Entity<UnrevivableComponent> ent, ref MapInitEvent args)
    {
        if (_activeTacticalMapTrackedQuery.TryComp(ent, out var active))
            UpdateTracked((ent, active));
    }

    private void OnUnrevivablRemove(Entity<UnrevivableComponent> ent, ref ComponentRemove args)
    {
        if (_activeTacticalMapTrackedQuery.TryComp(ent, out var active))
            UpdateTracked((ent, active));
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        _nextForceMapUpdate = TimeSpan.FromSeconds(30);
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
        RefreshUserVisibleLayers(ent);
        UpdateTacticalMapState(ent);
    }

    private void OnComputerBUIOpened(Entity<TacticalMapComputerComponent> ent, ref BoundUIOpenedEvent args)
    {
        RefreshComputerVisibleLayers(ent);
        if (_squad.TryGetMemberSquad(args.Actor, out var squad))
            ent.Comp.ObjectivesSquad = squad.Owner;
        else
            ent.Comp.ObjectivesSquad = null;

        UpdateTacticalMapComputerState(ent);
    }

    private void OnSquadObjectivesChanged(ref SquadObjectivesChangedEvent args)
    {
        if (_net.IsClient)
            return;

        var squad = args.Squad;
        var users = EntityQueryEnumerator<TacticalMapUserComponent>();
        while (users.MoveNext(out var member, out var user))
        {
            if (!_ui.IsUiOpen(member, TacticalMapUserUi.Key))
                continue;

            if (!_squad.IsInSquad((member, (SquadMemberComponent?) null), squad.Owner))
                continue;

            UpdateTacticalMapState((member, user));
        }

        var computers = EntityQueryEnumerator<TacticalMapComputerComponent>();
        while (computers.MoveNext(out var computerId, out var computer))
        {
            if (computer.ObjectivesSquad != squad.Owner)
                continue;

            if (_ui.IsUiOpen(computerId, TacticalMapComputerUi.Key))
                UpdateTacticalMapComputerState((computerId, computer));
        }
    }

    private void OnUserSelectMapMsg(Entity<TacticalMapUserComponent> ent, ref TacticalMapSelectMapMsg args)
    {
        if (!TryGetEntity(args.Map, out var mapEntity) ||
            mapEntity == null ||
            !_tacticalMapQuery.TryComp(mapEntity.Value, out var mapComp))
        {
            return;
        }

        ent.Comp.Map = mapEntity.Value;
        Dirty(ent);

        UpdateUserData(ent, mapComp);
        UpdateTacticalMapState(ent);
    }

    private void OnUserSelectLayerMsg(Entity<TacticalMapUserComponent> ent, ref TacticalMapSelectLayerMsg args)
    {
        if (!TryGetSelectedLayer(args.LayerId, out var selected))
            return;

        RefreshUserVisibleLayers(ent);
        var options = ent.Comp.LayerOptions;
        if (selected != null && !options.Contains(selected.Value))
            return;

        ent.Comp.ActiveLayer = selected;
        ApplyVisibleLayerSelection(ent, options);
        Dirty(ent);

        if (TryResolveUserMap(ent, out var map))
            UpdateUserData(ent, map.Comp);
    }

    private void OnUserSetVisibleLayersMsg(Entity<TacticalMapUserComponent> ent, ref TacticalMapSetVisibleLayersMsg args)
    {
        if (_net.IsClient)
            return;

        RefreshUserVisibleLayers(ent);
        var options = ent.Comp.LayerOptions;
        var selected = new List<ProtoId<TacticalMapLayerPrototype>>();
        foreach (var layerId in args.LayerIds)
        {
            if (_prototypes.HasIndex<TacticalMapLayerPrototype>(layerId) && options.Contains(layerId))
                selected.Add(layerId);
        }

        ent.Comp.VisibleLayers = selected;
        ApplyVisibleLayerSelection(ent, options);
        if (TryResolveUserMap(ent, out var map))
            UpdateUserData(ent, map.Comp);
    }

    private void OnComputerSelectMapMsg(Entity<TacticalMapComputerComponent> ent, ref TacticalMapSelectMapMsg args)
    {
        if (!TryGetEntity(args.Map, out var mapEntity) ||
            mapEntity == null ||
            !_tacticalMapQuery.TryComp(mapEntity.Value, out var mapComp))
        {
            return;
        }

        ent.Comp.Map = mapEntity.Value;
        Dirty(ent);

        UpdateMapData((ent, ent), mapComp);
        UpdateTacticalMapComputerState(ent);
    }

    private void OnComputerSelectLayerMsg(Entity<TacticalMapComputerComponent> ent, ref TacticalMapSelectLayerMsg args)
    {
        if (!TryGetSelectedLayer(args.LayerId, out var selected))
            return;

        RefreshComputerVisibleLayers(ent);
        var options = ent.Comp.LayerOptions;
        if (selected != null && !options.Contains(selected.Value))
            return;

        ent.Comp.ActiveLayer = selected;
        ApplyVisibleLayerSelection(ent, options);
        Dirty(ent);

        if (TryResolveComputerMap(ent, out var map))
            UpdateMapData(ent, map.Comp);
    }

    private void OnComputerSetVisibleLayersMsg(Entity<TacticalMapComputerComponent> ent, ref TacticalMapSetVisibleLayersMsg args)
    {
        if (_net.IsClient)
            return;

        RefreshComputerVisibleLayers(ent);
        var options = ent.Comp.LayerOptions;
        var selected = new List<ProtoId<TacticalMapLayerPrototype>>();
        foreach (var layerId in args.LayerIds)
        {
            if (_prototypes.HasIndex<TacticalMapLayerPrototype>(layerId) && options.Contains(layerId))
                selected.Add(layerId);
        }

        ent.Comp.VisibleLayers = selected;
        ApplyVisibleLayerSelection(ent, options);
        if (TryResolveComputerMap(ent, out var map))
            UpdateMapData(ent, map.Comp);
    }

    private void OnOverwatchBlipMsg(Entity<TacticalMapComputerComponent> ent, ref TacticalMapOverwatchBlipMsg args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp(ent.Owner, out OverwatchConsoleComponent? console))
        {
            _popup.PopupCursor("This map is not linked to an overwatch console.", args.Actor, PopupType.MediumCaution);
            return;
        }

        if (args.Target == NetEntity.Invalid || !TryGetEntity(args.Target, out var target))
        {
            _popup.PopupCursor("That blip is not a valid overwatch target.", args.Actor, PopupType.MediumCaution);
            return;
        }

        var bypassSquadCheck = HasComp<MarineCommunicationsComputerComponent>(ent.Owner);
        if (!bypassSquadCheck)
        {
            if (console.Squad is not { } squadNet ||
                !TryGetEntity(squadNet, out var squadEnt) ||
                !TryComp(squadEnt, out SquadTeamComponent? squadComp))
            {
                _popup.PopupCursor("No squad selected on the overwatch console.", args.Actor, PopupType.MediumCaution);
                return;
            }

            if (!_squad.IsInSquad((target.Value, (SquadMemberComponent?) null), squadEnt.Value))
            {
                _popup.PopupCursor("That marine is not in the selected squad.", args.Actor, PopupType.MediumCaution);
                return;
            }
        }

        if (!console.ShowHidden && _overwatch.IsHidden((ent.Owner, console), args.Target))
        {
            _popup.PopupCursor("That marine is hidden on the overwatch console.", args.Actor, PopupType.MediumCaution);
            return;
        }

        if (!_overwatch.TryWatchTarget(args.Actor, target.Value))
        {
            return;
        }
    }

    private void OnUserXenoWatchBlipMsg(Entity<TacticalMapUserComponent> ent, ref TacticalMapXenoWatchBlipMsg args)
    {
        if (_net.IsClient)
            return;

        if (!ent.Comp.LiveUpdate)
        {
            _popup.PopupCursor("The tactical map is not live right now.", args.Actor, PopupType.MediumCaution);
            return;
        }

        if (args.Target == NetEntity.Invalid || !TryGetEntity(args.Target, out var target))
        {
            _popup.PopupCursor("That blip is not a valid watch target.", args.Actor, PopupType.MediumCaution);
            return;
        }

        if (!HasComp<XenoComponent>(args.Actor))
        {
            _popup.PopupCursor("Only xenos can spectate from the tactical map.", args.Actor, PopupType.MediumCaution);
            return;
        }

        if (!HasComp<XenoComponent>(target.Value))
        {
            _popup.PopupCursor("That blip is not a xeno.", args.Actor, PopupType.MediumCaution);
            return;
        }

        TryComp(args.Actor, out HiveMemberComponent? watcherHive);
        TryComp(args.Actor, out ActorComponent? watcherActor);
        TryComp(args.Actor, out EyeComponent? watcherEye);
        TryComp(target.Value, out HiveMemberComponent? targetHive);

        _xenoWatch.Watch((args.Actor, watcherHive, watcherActor, watcherEye), (target.Value, targetHive));
    }

    private void UpdateTacticalMapState(Entity<TacticalMapUserComponent> ent)
    {
        var maps = BuildMapList();
        var activeMap = NetEntity.Invalid;
        if (TryResolveUserMap(ent, out var map))
            activeMap = GetNetEntity(map.Owner);

        var objectives = BuildObjectiveStateForUser(ent.Owner);
        var state = new TacticalMapBuiState(activeMap, maps, objectives);
        _ui.SetUiState(ent.Owner, TacticalMapUserUi.Key, state);
    }

    private void UpdateTacticalMapComputerState(Entity<TacticalMapComputerComponent> computer)
    {
        var maps = BuildMapList();
        var activeMap = NetEntity.Invalid;
        if (TryResolveComputerMap(computer, out var map))
            activeMap = GetNetEntity(map.Owner);

        var objectives = BuildObjectiveStateForComputer(computer);
        var state = new TacticalMapBuiState(activeMap, maps, objectives);
        _ui.SetUiState(computer.Owner, TacticalMapComputerUi.Key, state);
    }

    private Dictionary<SquadObjectiveType, string> BuildObjectiveState(SquadTeamComponent squad)
    {
        var objectives = new Dictionary<SquadObjectiveType, string>();
        foreach (var type in Enum.GetValues<SquadObjectiveType>())
        {
            objectives[type] = string.Empty;
        }

        foreach (var (type, objective) in squad.Objectives)
        {
            objectives[type] = objective ?? string.Empty;
        }

        return objectives;
    }

    private Dictionary<SquadObjectiveType, string> BuildObjectiveStateForUser(EntityUid viewer)
    {
        if (_squad.TryGetMemberSquad(viewer, out var squad))
            return BuildObjectiveState(squad.Comp);

        return new Dictionary<SquadObjectiveType, string>();
    }

    private Dictionary<SquadObjectiveType, string> BuildObjectiveStateForComputer(Entity<TacticalMapComputerComponent> computer)
    {
        if (computer.Comp.ObjectivesSquad != null &&
            TryComp(computer.Comp.ObjectivesSquad.Value, out SquadTeamComponent? squad))
        {
            return BuildObjectiveState(squad);
        }

        return new Dictionary<SquadObjectiveType, string>();
    }

    private void RefreshUserVisibleLayers(Entity<TacticalMapUserComponent> user)
    {
        var baseLayers = EnsureBaseLayers(user);
        var allowDefaults = !HasComp<RMCSurvivorComponent>(user.Owner);
        var options = BuildLayerOptions(user.Owner, baseLayers, includeLayerAccess: true, includeAllSquads: false, allowDefaultVisible: allowDefaults);
        ApplyLayerOptions(user, options);
        ApplyVisibleLayerSelection(user, options);
    }

    private void RefreshComputerVisibleLayers(Entity<TacticalMapComputerComponent> computer)
    {
        var baseLayers = EnsureBaseLayers(computer);
        var options = BuildLayerOptions(computer.Owner, baseLayers, includeLayerAccess: false, includeAllSquads: true);
        ApplyLayerOptions(computer, options);
        ApplyVisibleLayerSelection(computer, options);
        EnsureOverwatchDrawLayer(computer);
    }

    private IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> EnsureBaseLayers(Entity<TacticalMapUserComponent> user)
    {
        if (user.Comp.BaseLayers.Count == 0 && user.Comp.VisibleLayers.Count > 0)
        {
            user.Comp.BaseLayers = new List<ProtoId<TacticalMapLayerPrototype>>(user.Comp.VisibleLayers);
            Dirty(user);
        }

        return user.Comp.BaseLayers;
    }

    private IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> EnsureBaseLayers(Entity<TacticalMapComputerComponent> computer)
    {
        if (computer.Comp.BaseLayers.Count == 0 && computer.Comp.VisibleLayers.Count > 0)
        {
            computer.Comp.BaseLayers = new List<ProtoId<TacticalMapLayerPrototype>>(computer.Comp.VisibleLayers);
            Dirty(computer);
        }

        return computer.Comp.BaseLayers;
    }

    private List<ProtoId<TacticalMapLayerPrototype>> BuildLayerOptions(
        EntityUid? viewer,
        IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> baseLayers,
        bool includeLayerAccess,
        bool includeAllSquads,
        bool allowDefaultVisible = true)
    {
        var baseOrder = allowDefaultVisible
            ? GetVisibleLayers(baseLayers)
            : new List<ProtoId<TacticalMapLayerPrototype>>(baseLayers);
        var layers = new HashSet<ProtoId<TacticalMapLayerPrototype>>(baseOrder);

        if (includeLayerAccess && viewer != null)
            AddLayerAccessLayers(viewer.Value, layers);

        if (includeAllSquads)
            AddAllSquadLayers(layers);
        else if (viewer != null)
            AddViewerSquadLayer(viewer.Value, layers);

        return OrderLayers(layers, baseOrder);
    }

    private readonly HashSet<ProtoId<TacticalMapLayerPrototype>> _layerAccessBuffer = new();

    private void AddLayerAccessLayers(EntityUid user, HashSet<ProtoId<TacticalMapLayerPrototype>> layers)
    {
        if (!_layerAccess.TryGetLayers(user, _layerAccessBuffer))
            return;

        foreach (var layer in _layerAccessBuffer)
        {
            layers.Add(layer);
        }
    }

    private void AddViewerSquadLayer(EntityUid viewer, HashSet<ProtoId<TacticalMapLayerPrototype>> layers)
    {
        if (!_squad.TryGetMemberSquad(viewer, out var squad) ||
            !TryComp(squad, out SquadTeamComponent? squadComp) ||
            squadComp.TacticalMapLayer == null)
        {
            return;
        }

        layers.Add(squadComp.TacticalMapLayer.Value);
    }

    private void AddAllSquadLayers(HashSet<ProtoId<TacticalMapLayerPrototype>> layers)
    {
        var squads = EntityQueryEnumerator<SquadTeamComponent>();
        while (squads.MoveNext(out _, out var squad))
        {
            if (squad.TacticalMapLayer != null)
                layers.Add(squad.TacticalMapLayer.Value);
        }
    }

    private HashSet<ProtoId<TacticalMapLayerPrototype>> GetAllSquadLayers()
    {
        var layers = new HashSet<ProtoId<TacticalMapLayerPrototype>>();
        AddAllSquadLayers(layers);
        return layers;
    }

    private void EnsureOverwatchDrawLayer(Entity<TacticalMapComputerComponent> computer)
    {
        if (HasComp<MarineCommunicationsComputerComponent>(computer.Owner))
            return;

        if (!TryComp(computer.Owner, out OverwatchConsoleComponent? console))
            return;

        if (console.Squad is not { } squadNet ||
            !TryGetEntity(squadNet, out var squadEnt) ||
            !_squadTeamQuery.TryComp(squadEnt.Value, out var squadComp) ||
            squadComp.TacticalMapLayer == null)
        {
            return;
        }

        var layer = squadComp.TacticalMapLayer.Value;
        if (!computer.Comp.VisibleLayers.Contains(layer))
        {
            computer.Comp.VisibleLayers.Add(layer);
            ApplyVisibleLayerSelection(computer, computer.Comp.LayerOptions);
        }

        if (computer.Comp.ActiveLayer != layer)
        {
            computer.Comp.ActiveLayer = layer;
        }

        Dirty(computer);
    }

    private List<ProtoId<TacticalMapLayerPrototype>> OrderLayers(
        HashSet<ProtoId<TacticalMapLayerPrototype>> layers,
        IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> baseOrder)
    {
        var ordered = new List<ProtoId<TacticalMapLayerPrototype>>(layers.Count);
        foreach (var layer in baseOrder)
        {
            if (layers.Remove(layer))
                ordered.Add(layer);
        }

        if (layers.Count == 0)
            return ordered;

        var remaining = layers.ToList();
        remaining.Sort(CompareLayerOrder);
        ordered.AddRange(remaining);
        return ordered;
    }

    private int CompareLayerOrder(ProtoId<TacticalMapLayerPrototype> a, ProtoId<TacticalMapLayerPrototype> b)
    {
        var orderA = _prototypes.TryIndex(a, out var protoA) ? protoA.SortOrder : 0;
        var orderB = _prototypes.TryIndex(b, out var protoB) ? protoB.SortOrder : 0;
        var compare = orderA.CompareTo(orderB);
        return compare != 0 ? compare : string.CompareOrdinal(a.Id, b.Id);
    }

    private void ApplyLayerOptions(Entity<TacticalMapUserComponent> user, List<ProtoId<TacticalMapLayerPrototype>> options)
    {
        if (!options.SequenceEqual(user.Comp.LayerOptions))
        {
            user.Comp.LayerOptions = options;
            Dirty(user);
        }
    }

    private void ApplyLayerOptions(Entity<TacticalMapComputerComponent> computer, List<ProtoId<TacticalMapLayerPrototype>> options)
    {
        if (!options.SequenceEqual(computer.Comp.LayerOptions))
        {
            computer.Comp.LayerOptions = options;
            Dirty(computer);
        }
    }

    private void ApplyVisibleLayerSelection(Entity<TacticalMapUserComponent> user, IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> options)
    {
        var selected = user.Comp.VisibleLayers.Count == 0
            ? new List<ProtoId<TacticalMapLayerPrototype>>(options)
            : new List<ProtoId<TacticalMapLayerPrototype>>(user.Comp.VisibleLayers);

        selected = selected.Distinct().Where(options.Contains).ToList();
        if (selected.Count == 0 && options.Count > 0)
            selected.Add(options[0]);

        var ordered = OrderLayers(new HashSet<ProtoId<TacticalMapLayerPrototype>>(selected), options);
        if (!ordered.SequenceEqual(user.Comp.VisibleLayers))
        {
            user.Comp.VisibleLayers = ordered;
            Dirty(user);
        }

        EnsureActiveLayer(user, options);
    }

    private void ApplyVisibleLayerSelection(Entity<TacticalMapComputerComponent> computer, IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> options)
    {
        var selected = computer.Comp.VisibleLayers.Count == 0
            ? new List<ProtoId<TacticalMapLayerPrototype>>(options)
            : new List<ProtoId<TacticalMapLayerPrototype>>(computer.Comp.VisibleLayers);

        selected = selected.Distinct().Where(options.Contains).ToList();
        if (selected.Count == 0 && options.Count > 0)
            selected.Add(options[0]);

        var ordered = OrderLayers(new HashSet<ProtoId<TacticalMapLayerPrototype>>(selected), options);
        if (!ordered.SequenceEqual(computer.Comp.VisibleLayers))
        {
            computer.Comp.VisibleLayers = ordered;
            Dirty(computer);
        }

        EnsureActiveLayer(computer, options);
    }

    private void EnsureActiveLayer(Entity<TacticalMapUserComponent> user, IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> options)
    {
        if (user.Comp.ActiveLayer != null && options.Contains(user.Comp.ActiveLayer.Value))
            return;

        ProtoId<TacticalMapLayerPrototype>? fallback = null;
        if (options.Count > 0)
            fallback = options[0];

        user.Comp.ActiveLayer = user.Comp.VisibleLayers.Count > 0
            ? user.Comp.VisibleLayers[0]
            : fallback;
        Dirty(user);
    }

    private void EnsureActiveLayer(Entity<TacticalMapComputerComponent> computer, IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> options)
    {
        if (computer.Comp.ActiveLayer != null && options.Contains(computer.Comp.ActiveLayer.Value))
            return;

        ProtoId<TacticalMapLayerPrototype>? fallback = null;
        if (options.Count > 0)
            fallback = options[0];

        if (HasComp<MarineCommunicationsComputerComponent>(computer.Owner) &&
            options.Contains(GlobalMarineLayer))
        {
            computer.Comp.ActiveLayer = GlobalMarineLayer;
        }
        else
        {
            computer.Comp.ActiveLayer = computer.Comp.VisibleLayers.Count > 0
                ? computer.Comp.VisibleLayers[0]
                : fallback;
        }

        Dirty(computer);
    }

    private bool TryGetSelectedLayer(string? layerId, out ProtoId<TacticalMapLayerPrototype>? selected)
    {
        selected = null;
        if (string.IsNullOrWhiteSpace(layerId))
            return true;

        if (!_prototypes.HasIndex<TacticalMapLayerPrototype>(layerId))
            return false;

        selected = layerId;
        return true;
    }

    private bool TryGetDrawLayer(
        IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> visibleLayers,
        ProtoId<TacticalMapLayerPrototype>? activeLayer,
        out ProtoId<TacticalMapLayerPrototype> drawLayer)
    {
        if (activeLayer != null)
        {
            drawLayer = activeLayer.Value;
            return true;
        }

        var fallback = GetVisibleLayers(visibleLayers);
        if (fallback.Count == 0)
        {
            drawLayer = default;
            return false;
        }

        drawLayer = fallback[0];
        return true;
    }

    private List<ProtoId<TacticalMapLayerPrototype>> GetEffectiveVisibleLayers(
        IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> visibleLayers,
        ProtoId<TacticalMapLayerPrototype>? activeLayer,
        IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> options)
    {
        var effective = new List<ProtoId<TacticalMapLayerPrototype>>(GetVisibleLayers(visibleLayers));
        if (activeLayer != null && options.Contains(activeLayer.Value) && !effective.Contains(activeLayer.Value))
            effective.Add(activeLayer.Value);
        return effective;
    }

    private List<TacticalMapMapInfo> BuildMapList()
    {
        var maps = new List<TacticalMapMapInfo>();
        var query = EntityQueryEnumerator<TacticalMapComponent>();
        while (query.MoveNext(out var uid, out var map))
        {
            var mapId = ResolveMapId((uid, map));
            var displayName = ResolveMapDisplayName((uid, map), mapId);
            maps.Add(new TacticalMapMapInfo(GetNetEntity(uid), mapId, displayName));
        }

        maps.Sort(static (a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
        return maps;
    }

    private string ResolveMapId(Entity<TacticalMapComponent> map)
    {
        if (!string.IsNullOrWhiteSpace(map.Comp.MapId))
            return map.Comp.MapId;

        var meta = MetaData(map.Owner);
        if (!string.IsNullOrWhiteSpace(meta.EntityPrototype?.ID))
            return meta.EntityPrototype.ID;

        if (!string.IsNullOrWhiteSpace(meta.EntityName))
            return meta.EntityName;

        return map.Owner.ToString();
    }

    private string ResolveMapDisplayName(Entity<TacticalMapComponent> map, string mapId)
    {
        if (!string.IsNullOrWhiteSpace(map.Comp.DisplayName))
            return map.Comp.DisplayName;

        var meta = MetaData(map.Owner);
        if (!string.IsNullOrWhiteSpace(meta.EntityName))
            return meta.EntityName;

        return mapId;
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

        if (!TryResolveUserMap(ent, out var map))
            return;

        RefreshUserVisibleLayers(ent);
        var lines = args.Lines;
        if (lines.Count > LineLimit)
            lines = lines[..LineLimit];

        var labels = args.Labels;

        var time = _timing.CurTime;
        if (time < ent.Comp.NextAnnounceAt)
            return;

        var nextAnnounce = time + _announceCooldown;
        ent.Comp.LastAnnounceAt = time;
        ent.Comp.NextAnnounceAt = nextAnnounce;
        Dirty(ent);

        if (!TryGetDrawLayer(ent.Comp.VisibleLayers, ent.Comp.ActiveLayer, out var layer))
            return;

        var updateBlips = !HasComp<MarineComponent>(user);
        UpdateCanvas(map, lines, labels, layer, user, updateBlips, ent.Comp.Sound);
    }

    private void OnComputerUpdateCanvasMsg(Entity<TacticalMapComputerComponent> ent, ref TacticalMapUpdateCanvasMsg args)
    {
        var user = args.Actor;
        var isGroundside = HasComp<MarineCommunicationsComputerComponent>(ent.Owner);
        var isOverwatch = HasComp<OverwatchConsoleComponent>(ent.Owner);
        if (!isGroundside && !isOverwatch)
            return;

        if (!_skills.HasSkill(user, ent.Comp.Skill, ent.Comp.SkillLevel))
            return;

        if (!TryResolveComputerMap(ent, out var map))
            return;

        RefreshComputerVisibleLayers(ent);
        var lines = args.Lines;
        if (lines.Count > LineLimit)
            lines = lines[..LineLimit];

        var labels = args.Labels;

        if (!TryGetDrawLayer(ent.Comp.VisibleLayers, ent.Comp.ActiveLayer, out var layer))
            return;

        var updateBlips = isGroundside && layer == GlobalMarineLayer;
        var time = _timing.CurTime;
        if (updateBlips)
        {
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
        }
        UpdateCanvas(map, lines, labels, layer, user, updateBlips);
    }

    private void OnUserCreateLabelMsg(Entity<TacticalMapUserComponent> ent, ref TacticalMapCreateLabelMsg args)
    {
        var user = args.Actor;
        if (!ent.Comp.CanDraw)
            return;

        if (!TryResolveUserMap(ent, out var map))
            return;

        RefreshUserVisibleLayers(ent);
        var time = _timing.CurTime;
        if (time < ent.Comp.NextAnnounceAt)
            return;

        if (!TryGetDrawLayer(ent.Comp.VisibleLayers, ent.Comp.ActiveLayer, out var layer))
            return;

        UpdateIndividualLabel(map, layer, args.Position, args.Text, args.Color, user, LabelOperation.Create);
    }

    private void OnUserEditLabelMsg(Entity<TacticalMapUserComponent> ent, ref TacticalMapEditLabelMsg args)
    {
        var user = args.Actor;
        if (!ent.Comp.CanDraw)
            return;

        if (!TryResolveUserMap(ent, out var map))
            return;

        RefreshUserVisibleLayers(ent);
        var time = _timing.CurTime;
        if (time < ent.Comp.NextAnnounceAt)
            return;

        if (!TryGetDrawLayer(ent.Comp.VisibleLayers, ent.Comp.ActiveLayer, out var layer))
            return;

        UpdateIndividualLabel(map, layer, args.Position, args.NewText, null, user, LabelOperation.Edit);
    }

    private void OnUserDeleteLabelMsg(Entity<TacticalMapUserComponent> ent, ref TacticalMapDeleteLabelMsg args)
    {
        var user = args.Actor;
        if (!ent.Comp.CanDraw)
            return;

        if (!TryResolveUserMap(ent, out var map))
            return;

        RefreshUserVisibleLayers(ent);
        var time = _timing.CurTime;
        if (time < ent.Comp.NextAnnounceAt)
            return;

        if (!TryGetDrawLayer(ent.Comp.VisibleLayers, ent.Comp.ActiveLayer, out var layer))
            return;

        UpdateIndividualLabel(map, layer, args.Position, string.Empty, null, user, LabelOperation.Delete);
    }

    private void OnUserMoveLabelMsg(Entity<TacticalMapUserComponent> ent, ref TacticalMapMoveLabelMsg args)
    {
        var user = args.Actor;
        if (!ent.Comp.CanDraw)
            return;

        if (!TryResolveUserMap(ent, out var map))
            return;

        RefreshUserVisibleLayers(ent);
        var time = _timing.CurTime;
        if (time < ent.Comp.NextAnnounceAt)
            return;

        if (!TryGetDrawLayer(ent.Comp.VisibleLayers, ent.Comp.ActiveLayer, out var layer))
            return;

        UpdateMoveLabel(map, layer, args.OldPosition, args.NewPosition, user);
    }

    private void OnComputerCreateLabelMsg(Entity<TacticalMapComputerComponent> ent, ref TacticalMapCreateLabelMsg args)
    {
        var user = args.Actor;
        if (!_skills.HasSkill(user, ent.Comp.Skill, ent.Comp.SkillLevel))
            return;

        if (!TryResolveComputerMap(ent, out var map))
            return;

        RefreshComputerVisibleLayers(ent);
        var time = _timing.CurTime;
        if (time < ent.Comp.NextAnnounceAt)
            return;

        if (!TryGetDrawLayer(ent.Comp.VisibleLayers, ent.Comp.ActiveLayer, out var layer))
            return;

        UpdateIndividualLabel(map, layer, args.Position, args.Text, args.Color, user, LabelOperation.Create);
    }

    private void OnComputerEditLabelMsg(Entity<TacticalMapComputerComponent> ent, ref TacticalMapEditLabelMsg args)
    {
        var user = args.Actor;
        if (!_skills.HasSkill(user, ent.Comp.Skill, ent.Comp.SkillLevel))
            return;

        if (!TryResolveComputerMap(ent, out var map))
            return;

        RefreshComputerVisibleLayers(ent);
        var time = _timing.CurTime;
        if (time < ent.Comp.NextAnnounceAt)
            return;

        if (!TryGetDrawLayer(ent.Comp.VisibleLayers, ent.Comp.ActiveLayer, out var layer))
            return;

        UpdateIndividualLabel(map, layer, args.Position, args.NewText, null, user, LabelOperation.Edit);
    }

    private void OnComputerDeleteLabelMsg(Entity<TacticalMapComputerComponent> ent, ref TacticalMapDeleteLabelMsg args)
    {
        var user = args.Actor;
        if (!_skills.HasSkill(user, ent.Comp.Skill, ent.Comp.SkillLevel))
            return;

        if (!TryResolveComputerMap(ent, out var map))
            return;

        RefreshComputerVisibleLayers(ent);
        var time = _timing.CurTime;
        if (time < ent.Comp.NextAnnounceAt)
            return;

        if (!TryGetDrawLayer(ent.Comp.VisibleLayers, ent.Comp.ActiveLayer, out var layer))
            return;

        UpdateIndividualLabel(map, layer, args.Position, string.Empty, null, user, LabelOperation.Delete);
    }

    private void OnComputerMoveLabelMsg(Entity<TacticalMapComputerComponent> ent, ref TacticalMapMoveLabelMsg args)
    {
        var user = args.Actor;
        if (!_skills.HasSkill(user, ent.Comp.Skill, ent.Comp.SkillLevel))
            return;

        if (!TryResolveComputerMap(ent, out var map))
            return;

        RefreshComputerVisibleLayers(ent);
        var time = _timing.CurTime;
        if (time < ent.Comp.NextAnnounceAt)
            return;

        if (!TryGetDrawLayer(ent.Comp.VisibleLayers, ent.Comp.ActiveLayer, out var layer))
            return;

        UpdateMoveLabel(map, layer, args.OldPosition, args.NewPosition, user);
    }

    private enum LabelOperation
    {
        Create,
        Edit,
        Delete
    }

    private void OnUserQueenEyeMoveMsg(Entity<TacticalMapUserComponent> ent, ref TacticalMapQueenEyeMoveMsg args)
    {
        HandleQueenEyeMove(ent, args.Actor, args.Position);
    }

    private void HandleQueenEyeMove(Entity<TacticalMapUserComponent> user, EntityUid actor, Vector2i position)
    {
        if (HasComp<GhostComponent>(actor))
        {
            if (!TryResolveUserMap(user, out var ghostMap) ||
                !_mapGridQuery.TryComp(ghostMap.Owner, out var ghostGrid))
            {
                return;
            }

            var ghostTileCoords = new Vector2(position.X, position.Y);
            var coords = new EntityCoordinates(ghostMap.Owner, ghostTileCoords * ghostGrid.TileSize);
            _transform.SetCoordinates(actor, coords);
            _transform.AttachToGridOrMap(actor);

            if (TryComp(actor, out PhysicsComponent? physics))
                _physics.SetLinearVelocity(actor, Vector2.Zero, body: physics);

            return;
        }

        if (!TryComp<QueenEyeActionComponent>(actor, out var queenEyeComp) ||
            queenEyeComp.Eye == null)
            return;

        var eye = queenEyeComp.Eye.Value;

        if (!TryResolveUserMap(user, out var map) ||
            !TryComp<MapGridComponent>(map.Owner, out var grid))
            return;

        var queenTransform = Transform(actor);
        var eyeTransform = Transform(eye);
        var mapTransform = Transform(map.Owner);

        if (queenTransform.MapID != mapTransform.MapID)
            return;

        var tileCoords = new Vector2(position.X, position.Y);
        var worldPos = _transform.ToMapCoordinates(new EntityCoordinates(map.Owner, tileCoords * grid.TileSize));

        _transform.SetWorldPosition(eye, worldPos.Position);
    }

    public override void OpenComputerMap(Entity<TacticalMapComputerComponent?> computer, EntityUid user)
    {
        if (!Resolve(computer, ref computer.Comp, false))
            return;

        if (_squad.TryGetMemberSquad(user, out var squad))
            computer.Comp.ObjectivesSquad = squad.Owner;
        else
            computer.Comp.ObjectivesSquad = null;

        _ui.TryOpenUi(computer.Owner, TacticalMapComputerUi.Key, user);
        UpdateMapData((computer, computer.Comp));
        UpdateTacticalMapComputerState((computer.Owner, computer.Comp));
    }

    public override void SetComputerDrawLayerFromSquad(EntityUid computer, EntityUid squad)
    {
        if (!TryComp(computer, out TacticalMapComputerComponent? mapComp))
            return;

        if (!_squadTeamQuery.TryComp(squad, out var squadComp) || squadComp.TacticalMapLayer == null)
            return;

        var layer = squadComp.TacticalMapLayer.Value;
        if (!mapComp.LayerOptions.Contains(layer))
            mapComp.LayerOptions.Add(layer);

        if (!mapComp.VisibleLayers.Contains(layer))
            mapComp.VisibleLayers.Add(layer);

        mapComp.ActiveLayer = layer;
        Dirty(computer, mapComp);

        ApplyVisibleLayerSelection((computer, mapComp), mapComp.LayerOptions);

        if (TryResolveComputerMap((computer, mapComp), out var map))
            UpdateMapData((computer, mapComp), map.Comp);
    }

    private void UpdateIndividualLabel(
        Entity<TacticalMapComponent> map,
        ProtoId<TacticalMapLayerPrototype> layer,
        Vector2i position,
        string text,
        Color? color,
        EntityUid user,
        LabelOperation operation)
    {
        var layerData = EnsureLayer(map.Comp, layer);
        map.Comp.MapDirty = true;

        switch (operation)
        {
            case LabelOperation.Create:
            case LabelOperation.Edit:
                if (string.IsNullOrWhiteSpace(text))
                {
                    layerData.Labels.Remove(position);
                    break;
                }

                var labelColor = color ?? (layerData.Labels.TryGetValue(position, out var existing)
                    ? existing.Color
                    : Color.White);
                layerData.Labels[position] = new TacticalMapLabelData(text, labelColor);
                break;
            case LabelOperation.Delete:
                layerData.Labels.Remove(position);
                break;
        }

        _adminLog.Add(LogType.RMCTacticalMapUpdated,
            $"{ToPrettyString(user)} {operation.ToString().ToLower()}d a {GetLayerLogName(layer)} tactical map label at {position} for {ToPrettyString(map.Owner)}");
    }

    private void UpdateMoveLabel(Entity<TacticalMapComponent> map, ProtoId<TacticalMapLayerPrototype> layer, Vector2i oldPosition, Vector2i newPosition, EntityUid user)
    {
        var layerData = EnsureLayer(map.Comp, layer);
        map.Comp.MapDirty = true;

        if (!layerData.Labels.TryGetValue(oldPosition, out var data))
            return;

        layerData.Labels.Remove(oldPosition);
        layerData.Labels[newPosition] = data;

        _adminLog.Add(LogType.RMCTacticalMapUpdated,
            $"{ToPrettyString(user)} moved a {GetLayerLogName(layer)} tactical map label from {oldPosition} to {newPosition} for {ToPrettyString(map.Owner)}");
    }

    private TacticalMapLayerData EnsureLayer(TacticalMapComponent map, ProtoId<TacticalMapLayerPrototype> layer)
    {
        if (!map.Layers.TryGetValue(layer, out var data))
        {
            data = new TacticalMapLayerData();
            map.Layers[layer] = data;
        }

        return data;
    }

    private string GetLayerLogName(ProtoId<TacticalMapLayerPrototype> layer)
    {
        if (_prototypes.TryIndex(layer, out var proto) && !string.IsNullOrWhiteSpace(proto.LogName))
            return proto.LogName;

        return layer.Id;
    }

    private void AddAlwaysVisibleBlips(ProtoId<TacticalMapLayerPrototype> layer, TacticalMapComponent map, Dictionary<int, TacticalMapBlip> blips)
    {
        var alwaysVisible = EntityQueryEnumerator<TacticalMapAlwaysVisibleComponent>();
        while (alwaysVisible.MoveNext(out var uid, out var comp))
        {
            if (!comp.VisibleLayers.Contains(layer) || blips.ContainsKey(uid.Id))
                continue;

            var blip = FindBlipInMap(uid.Id, map);
            if (blip == null)
                continue;

            blips[uid.Id] = blip.Value;
        }
    }

    private bool TryGetGridCoordinates(Vector2i tacticalPosition, out EntityCoordinates coordinates)
    {
        coordinates = default;

        var maps = EntityQueryEnumerator<TacticalMapComponent>();
        while (maps.MoveNext(out var mapId, out var map))
        {
            if (!_transformQuery.TryComp(mapId, out var mapTransform) ||
                !_mapGridQuery.TryComp(mapId, out var mapGrid))
            {
                continue;
            }

            coordinates = new EntityCoordinates(mapId, new Vector2(tacticalPosition.X, tacticalPosition.Y));
            return true;
        }

        return false;
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

        foreach (var layerData in tacticalMap.Layers.Values)
        {
            layerData.Blips.Remove(tracked.Owner.Id);
        }

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
        if (_mobState.IsDead(ent))
        {
            var stage = _unrevivableSystem.GetUnrevivableStage(ent.Owner, 5);
            if (_rottingQuery.HasComp(ent) || _unrevivableSystem.IsUnrevivable(ent))
                status = TacticalMapBlipStatus.Undefibabble;
            else if (stage <= 1)
                status = TacticalMapBlipStatus.Defibabble;
            else if (stage == 2)
                status = TacticalMapBlipStatus.Defibabble2;
            else if (stage == 3)
                status = TacticalMapBlipStatus.Defibabble3;
            else if (stage == 4)
                status = TacticalMapBlipStatus.Defibabble4;
        }

        var blip = new TacticalMapBlip(indices, icon, ent.Comp.Color, status, ent.Comp.Background, ent.Comp.HiveLeader);
        var updated = false;
        var layerIds = new HashSet<ProtoId<TacticalMapLayerPrototype>>();

        if (_tacticalMapLayerTrackedQuery.TryComp(ent, out var layerTracked) &&
            layerTracked.Layers.Count > 0)
        {
            foreach (var layerId in layerTracked.Layers)
            {
                layerIds.Add(layerId);
            }
        }

        AddLayerAccessLayers(ent.Owner, layerIds);

        foreach (var layerId in layerIds)
        {
            var layerData = EnsureLayer(tacticalMap, layerId);
            layerData.Blips[ent.Owner.Id] = blip;
            updated = true;
        }

        if (updated)
            tacticalMap.MapDirty = true;
    }

    public override void UpdateUserData(Entity<TacticalMapUserComponent> user, TacticalMapComponent map)
    {
        var lines = EnsureComp<TacticalMapLinesComponent>(user);
        var labels = EnsureComp<TacticalMapLabelsComponent>(user);

        RefreshUserVisibleLayers(user);
        var visibleLayers = GetEffectiveVisibleLayers(user.Comp.VisibleLayers, user.Comp.ActiveLayer, user.Comp.LayerOptions);
        var blips = new Dictionary<int, TacticalMapBlip>();

        foreach (var layer in visibleLayers)
        {
            if (map.Layers.TryGetValue(layer, out var layerData))
            {
                var sourceBlips = user.Comp.LiveUpdate ? layerData.Blips : layerData.LastUpdateBlips;
                foreach (var (id, blip) in sourceBlips)
                {
                    blips.TryAdd(id, blip);
                }
            }

            AddAlwaysVisibleBlips(layer, map, blips);
        }

        AddSelfBlip(user.Owner, map, blips);
        FilterXenoBlipsForHive(user.Owner, blips);
        user.Comp.Blips = blips;

        var combinedLines = new List<TacticalMapLine>();
        var combinedLabels = new Dictionary<Vector2i, TacticalMapLabelData>();

        foreach (var layer in visibleLayers)
        {
            if (!map.Layers.TryGetValue(layer, out var layerData))
                continue;

            combinedLines.AddRange(layerData.Lines);
            foreach (var (pos, label) in layerData.Labels)
            {
                combinedLabels[pos] = label;
            }
        }

        lines.Lines = combinedLines;
        labels.Labels = combinedLabels;

        var activeLines = EnsureComp<TacticalMapActiveLayerLinesComponent>(user);
        var activeLabels = EnsureComp<TacticalMapActiveLayerLabelsComponent>(user);
        var activeLayers = GetActiveLayers(visibleLayers, user.Comp.ActiveLayer);
        var activeLayerLines = new List<TacticalMapLine>();
        var activeLayerLabels = new Dictionary<Vector2i, TacticalMapLabelData>();

        foreach (var layer in activeLayers)
        {
            if (!map.Layers.TryGetValue(layer, out var layerData))
                continue;

            activeLayerLines.AddRange(layerData.Lines);
            foreach (var (pos, label) in layerData.Labels)
            {
                activeLayerLabels[pos] = label;
            }
        }

        activeLines.Lines = activeLayerLines;
        activeLabels.Labels = activeLayerLabels;

        Dirty(user);
        Dirty(user, lines);
        Dirty(user, labels);
        Dirty(user, activeLines);
        Dirty(user, activeLabels);
    }

    private void FilterXenoBlipsForHive(EntityUid viewer, Dictionary<int, TacticalMapBlip> blips)
    {
        if (!HasComp<XenoComponent>(viewer))
            return;

        TryComp(viewer, out HiveMemberComponent? viewerHive);
        var viewerHiveId = viewerHive?.Hive;

        var toRemove = new List<int>();
        foreach (var (entityId, _) in blips)
        {
            if (entityId == viewer.Id)
                continue;

            var target = new EntityUid(entityId);
            if (TerminatingOrDeleted(target))
                continue;

            if (!HasComp<XenoComponent>(target))
                continue;

            if (!TryComp(target, out HiveMemberComponent? targetHive) ||
                viewerHiveId == null ||
                targetHive.Hive == null ||
                targetHive.Hive != viewerHiveId)
            {
                toRemove.Add(entityId);
            }
        }

        foreach (var entityId in toRemove)
        {
            blips.Remove(entityId);
        }
    }

    private TacticalMapBlip? FindBlipInMap(int entityId, TacticalMapComponent map)
    {
        foreach (var layer in map.Layers.Values)
        {
            if (layer.Blips.TryGetValue(entityId, out var blip))
                return blip;
        }

        return null;
    }

    private void AddSelfBlip(EntityUid user, TacticalMapComponent map, Dictionary<int, TacticalMapBlip> blips)
    {
        if (blips.ContainsKey(user.Id))
            return;

        var blip = FindBlipInMap(user.Id, map);
        if (blip == null)
            return;

        blips[user.Id] = blip.Value;
    }

    private void UpdateCanvas(Entity<TacticalMapComponent> map, List<TacticalMapLine> lines, Dictionary<Vector2i, TacticalMapLabelData> labels, ProtoId<TacticalMapLayerPrototype> layer, EntityUid user, bool updateBlips, SoundSpecifier? sound = null)
    {
        var layerData = EnsureLayer(map.Comp, layer);
        map.Comp.MapDirty = true;

        layerData.Lines = lines;
        layerData.Labels = new Dictionary<Vector2i, TacticalMapLabelData>(labels);
        if (updateBlips)
        {
            layerData.LastUpdateBlips = layerData.Blips.ToDictionary();

            var snapshotLayers = ApplyLayerVisibilityRules(user, new[] { layer });
            foreach (var extraLayer in snapshotLayers)
            {
                if (extraLayer == layer)
                    continue;

                if (!map.Comp.Layers.TryGetValue(extraLayer, out var extraLayerData))
                    continue;

                foreach (var blip in extraLayerData.Blips)
                {
                    layerData.LastUpdateBlips.TryAdd(blip.Key, blip.Value);
                }
            }
        }

        AnnounceLayerUpdate(layer, user, map.Owner, sound);

        var ev = new TacticalMapUpdatedEvent(lines.ToList(), user);
        RaiseLocalEvent(ref ev);
    }

    private void AnnounceLayerUpdate(ProtoId<TacticalMapLayerPrototype> layer, EntityUid user, EntityUid mapOwner, SoundSpecifier? sound)
    {
        if (_prototypes.TryIndex(layer, out var proto) && !string.IsNullOrWhiteSpace(proto.UpdateAnnouncement))
        {
            switch (proto.AnnouncementTarget)
            {
                case TacticalMapLayerAnnouncementTarget.Marines:
                    _marineAnnounce.AnnounceARESStaging(user, proto.UpdateAnnouncement, sound);
                    break;
                case TacticalMapLayerAnnouncementTarget.Xenos:
                    _xenoAnnounce.AnnounceSameHive(user, proto.UpdateAnnouncement, sound);
                    break;
            }
        }

        _adminLog.Add(LogType.RMCTacticalMapUpdated,
            $"{ToPrettyString(user)} updated the {GetLayerLogName(layer)} tactical map for {ToPrettyString(mapOwner)}");
    }

    protected override void UpdateMapData(Entity<TacticalMapComputerComponent> computer, TacticalMapComponent map)
    {
        RefreshComputerVisibleLayers(computer);
        var visibleLayers = GetEffectiveVisibleLayers(computer.Comp.VisibleLayers, computer.Comp.ActiveLayer, computer.Comp.LayerOptions);
        var blipLayers = ApplyLayerVisibilityRules(computer.Owner, visibleLayers);

        var blips = new Dictionary<int, TacticalMapBlip>();
        foreach (var layer in blipLayers)
        {
            if (!map.Layers.TryGetValue(layer, out var layerData))
                continue;

            foreach (var (id, blip) in layerData.Blips)
            {
                blips.TryAdd(id, blip);
            }
        }

        computer.Comp.Blips = blips;
        Dirty(computer);

        var lines = EnsureComp<TacticalMapLinesComponent>(computer);
        var labels = EnsureComp<TacticalMapLabelsComponent>(computer);

        var combinedLines = new List<TacticalMapLine>();
        var combinedLabels = new Dictionary<Vector2i, TacticalMapLabelData>();

        foreach (var layer in visibleLayers)
        {
            if (!map.Layers.TryGetValue(layer, out var layerData))
                continue;

            combinedLines.AddRange(layerData.Lines);
            foreach (var (pos, label) in layerData.Labels)
            {
                combinedLabels[pos] = label;
            }
        }

        lines.Lines = combinedLines;
        labels.Labels = combinedLabels;

        var activeLines = EnsureComp<TacticalMapActiveLayerLinesComponent>(computer);
        var activeLabels = EnsureComp<TacticalMapActiveLayerLabelsComponent>(computer);
        var activeLayers = GetActiveLayers(visibleLayers, computer.Comp.ActiveLayer);
        var activeLayerLines = new List<TacticalMapLine>();
        var activeLayerLabels = new Dictionary<Vector2i, TacticalMapLabelData>();

        foreach (var layer in activeLayers)
        {
            if (!map.Layers.TryGetValue(layer, out var layerData))
                continue;

            activeLayerLines.AddRange(layerData.Lines);
            foreach (var (pos, label) in layerData.Labels)
            {
                activeLayerLabels[pos] = label;
            }
        }

        activeLines.Lines = activeLayerLines;
        activeLabels.Labels = activeLayerLabels;

        Dirty(computer, lines);
        Dirty(computer, labels);
        Dirty(computer, activeLines);
        Dirty(computer, activeLabels);
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

        var time = _timing.CurTime;
        if (time > _nextForceMapUpdate)
        {
            _nextForceMapUpdate = time + _forceMapUpdateEvery;
            var tracked = EntityQueryEnumerator<ActiveTacticalMapTrackedComponent>();
            while (tracked.MoveNext(out var ent, out var comp))
            {
                if (comp == null)
                    continue;

                _toUpdate.Add((ent, comp));
            }
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

        var maps = EntityQueryEnumerator<TacticalMapComponent>();
        while (maps.MoveNext(out var mapId, out var map))
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

                if (computer.Map != mapId)
                    continue;

                UpdateMapData((computerId, computer), map);
            }

            var users = EntityQueryEnumerator<ActiveTacticalMapUserComponent, TacticalMapUserComponent>();
            while (users.MoveNext(out var userId, out _, out var userComp))
            {
                if (userComp.Map != mapId)
                    continue;

                UpdateUserData((userId, userComp), map);
            }

            var tunnelUsers = EntityQueryEnumerator<TunnelUIUserComponent, TacticalMapUserComponent>();
            while (tunnelUsers.MoveNext(out var tunnelUserId, out _, out var tunnelUserComp))
            {
                if (tunnelUserComp.Map != mapId)
                    continue;

                UpdateUserData((tunnelUserId, tunnelUserComp), map);
            }

            var dropshipWeapons = EntityQueryEnumerator<TacticalMapComputerComponent, DropshipTerminalWeaponsComponent>();
            while (dropshipWeapons.MoveNext(out var weaponsId, out var weaponsComputer, out var weapons))
            {
                if (!_ui.IsUiOpen(weaponsId, DropshipTerminalWeaponsUi.Key))
                    continue;

                if (weaponsComputer.Map != mapId)
                    continue;

                UpdateMapData((weaponsId, weaponsComputer), map);
            }

            _replay.RecordSnapshot(mapId, map);
        }
    }
}
