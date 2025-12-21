using System;
using System.Linq;
using System.Numerics;
using Content.Server._RMC14.Announce;
using Content.Server._RMC14.Marines;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking.Events;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Medical.Unrevivable;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Eye;
using Content.Shared._RMC14.Xenonids.HiveLeader;
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
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.TacticalMap;

public sealed class TacticalMapSystem : SharedTacticalMapSystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly XenoEvolutionSystem _evolution = default!;
    [Dependency] private readonly GunIFFSystem _gunIff = default!;
    [Dependency] private readonly MarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly XenoAnnounceSystem _xenoAnnounce = default!;
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
            });

        Subs.BuiEvents<TacticalMapComputerComponent>(TacticalMapComputerUi.Key,
            subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnComputerBUIOpened);
                subs.Event<TacticalMapSelectMapMsg>(OnComputerSelectMapMsg);
                subs.Event<TacticalMapSelectLayerMsg>(OnComputerSelectLayerMsg);
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
        UpdateTacticalMapComputerState(ent);
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
        var visibleLayers = GetVisibleLayers(ent.Comp.VisibleLayers);
        if (selected != null && !visibleLayers.Contains(selected.Value))
            return;

        ent.Comp.ActiveLayer = selected;
        Dirty(ent);

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
        var visibleLayers = GetVisibleLayers(ent.Comp.VisibleLayers);
        if (selected != null && !visibleLayers.Contains(selected.Value))
            return;

        ent.Comp.ActiveLayer = selected;
        Dirty(ent);

        if (TryResolveComputerMap(ent, out var map))
            UpdateMapData(ent, map.Comp);
    }

    private void UpdateTacticalMapState(Entity<TacticalMapUserComponent> ent)
    {
        var maps = BuildMapList();
        var activeMap = NetEntity.Invalid;
        if (TryResolveUserMap(ent, out var map))
            activeMap = GetNetEntity(map.Owner);

        var state = new TacticalMapBuiState(activeMap, maps);
        _ui.SetUiState(ent.Owner, TacticalMapUserUi.Key, state);
    }

    private void UpdateTacticalMapComputerState(Entity<TacticalMapComputerComponent> computer)
    {
        var maps = BuildMapList();
        var activeMap = NetEntity.Invalid;
        if (TryResolveComputerMap(computer, out var map))
            activeMap = GetNetEntity(map.Owner);

        var state = new TacticalMapBuiState(activeMap, maps);
        _ui.SetUiState(computer.Owner, TacticalMapComputerUi.Key, state);
    }

    private void RefreshUserVisibleLayers(Entity<TacticalMapUserComponent> user)
    {
        var baseLayers = EnsureBaseLayers(user);
        var ordered = BuildVisibleLayers(user.Owner, baseLayers, includeIff: true);
        ApplyVisibleLayers(user, ordered);
    }

    private void RefreshComputerVisibleLayers(Entity<TacticalMapComputerComponent> computer)
    {
        var baseLayers = EnsureBaseLayers(computer);
        var ordered = BuildVisibleLayers(computer.Owner, baseLayers, includeIff: false);
        ApplyVisibleLayers(computer, ordered);
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

    private List<ProtoId<TacticalMapLayerPrototype>> BuildVisibleLayers(
        EntityUid? viewer,
        IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> baseLayers,
        bool includeIff)
    {
        var baseOrder = GetVisibleLayers(baseLayers);
        var layers = new HashSet<ProtoId<TacticalMapLayerPrototype>>(baseOrder);

        if (includeIff && viewer != null)
            AddIffLayers(viewer.Value, layers);

        return OrderLayers(layers, baseOrder);
    }

    private void AddIffLayers(EntityUid user, HashSet<ProtoId<TacticalMapLayerPrototype>> layers)
    {
        if (!_gunIff.TryGetFaction(user, out var faction))
            return;

        foreach (var layer in _prototypes.EnumeratePrototypes<TacticalMapLayerPrototype>())
        {
            if (layer.IffFactions == null || !layer.IffFactions.Contains(faction))
                continue;

            layers.Add(layer.ID);
        }
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

    private void ApplyVisibleLayers(Entity<TacticalMapUserComponent> user, List<ProtoId<TacticalMapLayerPrototype>> ordered)
    {
        if (!ordered.SequenceEqual(user.Comp.VisibleLayers))
        {
            user.Comp.VisibleLayers = ordered;
            Dirty(user);
        }

        if (user.Comp.ActiveLayer != null && !user.Comp.VisibleLayers.Contains(user.Comp.ActiveLayer.Value))
        {
            user.Comp.ActiveLayer = null;
            Dirty(user);
        }
    }

    private void ApplyVisibleLayers(Entity<TacticalMapComputerComponent> computer, List<ProtoId<TacticalMapLayerPrototype>> ordered)
    {
        if (!ordered.SequenceEqual(computer.Comp.VisibleLayers))
        {
            computer.Comp.VisibleLayers = ordered;
            Dirty(computer);
        }

        if (computer.Comp.ActiveLayer != null && !computer.Comp.VisibleLayers.Contains(computer.Comp.ActiveLayer.Value))
        {
            computer.Comp.ActiveLayer = null;
            Dirty(computer);
        }
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

        foreach (var layer in GetActiveLayers(ent.Comp.VisibleLayers, ent.Comp.ActiveLayer))
        {
            UpdateCanvas(map, lines, labels, layer, user, ent.Comp.Sound);
        }
    }

    private void OnComputerUpdateCanvasMsg(Entity<TacticalMapComputerComponent> ent, ref TacticalMapUpdateCanvasMsg args)
    {
        var user = args.Actor;
        if (!_skills.HasSkill(user, ent.Comp.Skill, ent.Comp.SkillLevel))
            return;

        if (!TryResolveComputerMap(ent, out var map))
            return;

        RefreshComputerVisibleLayers(ent);
        var lines = args.Lines;
        if (lines.Count > LineLimit)
            lines = lines[..LineLimit];

        var labels = args.Labels;

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

        foreach (var layer in GetActiveLayers(ent.Comp.VisibleLayers, ent.Comp.ActiveLayer))
        {
            UpdateCanvas(map, lines, labels, layer, user);
        }
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

        foreach (var layer in GetActiveLayers(ent.Comp.VisibleLayers, ent.Comp.ActiveLayer))
        {
            UpdateIndividualLabel(map, layer, args.Position, args.Text, user, LabelOperation.Create);
        }
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

        foreach (var layer in GetActiveLayers(ent.Comp.VisibleLayers, ent.Comp.ActiveLayer))
        {
            UpdateIndividualLabel(map, layer, args.Position, args.NewText, user, LabelOperation.Edit);
        }
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

        foreach (var layer in GetActiveLayers(ent.Comp.VisibleLayers, ent.Comp.ActiveLayer))
        {
            UpdateIndividualLabel(map, layer, args.Position, string.Empty, user, LabelOperation.Delete);
        }
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

        foreach (var layer in GetActiveLayers(ent.Comp.VisibleLayers, ent.Comp.ActiveLayer))
        {
            UpdateMoveLabel(map, layer, args.OldPosition, args.NewPosition, user);
        }
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

        foreach (var layer in GetActiveLayers(ent.Comp.VisibleLayers, ent.Comp.ActiveLayer))
        {
            UpdateIndividualLabel(map, layer, args.Position, args.Text, user, LabelOperation.Create);
        }
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

        foreach (var layer in GetActiveLayers(ent.Comp.VisibleLayers, ent.Comp.ActiveLayer))
        {
            UpdateIndividualLabel(map, layer, args.Position, args.NewText, user, LabelOperation.Edit);
        }
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

        foreach (var layer in GetActiveLayers(ent.Comp.VisibleLayers, ent.Comp.ActiveLayer))
        {
            UpdateIndividualLabel(map, layer, args.Position, string.Empty, user, LabelOperation.Delete);
        }
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

        foreach (var layer in GetActiveLayers(ent.Comp.VisibleLayers, ent.Comp.ActiveLayer))
        {
            UpdateMoveLabel(map, layer, args.OldPosition, args.NewPosition, user);
        }
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

        _ui.TryOpenUi(computer.Owner, TacticalMapComputerUi.Key, user);
        UpdateMapData((computer, computer.Comp));
        UpdateTacticalMapComputerState((computer.Owner, computer.Comp));
    }

    private void UpdateIndividualLabel(Entity<TacticalMapComponent> map, ProtoId<TacticalMapLayerPrototype> layer, Vector2i position, string text, EntityUid user, LabelOperation operation)
    {
        var layerData = EnsureLayer(map.Comp, layer);
        map.Comp.MapDirty = true;

        switch (operation)
        {
            case LabelOperation.Create:
            case LabelOperation.Edit:
                if (string.IsNullOrWhiteSpace(text))
                    layerData.Labels.Remove(position);
                else
                    layerData.Labels[position] = text;
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

        if (!layerData.Labels.TryGetValue(oldPosition, out var text))
            return;

        layerData.Labels.Remove(oldPosition);
        layerData.Labels[newPosition] = text;

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

        if (_tacticalMapLayerTrackedQuery.TryComp(ent, out var layerTracked) &&
            layerTracked.Layers.Count > 0)
        {
            foreach (var layerId in layerTracked.Layers)
            {
                var layerData = EnsureLayer(tacticalMap, layerId);
                layerData.Blips[ent.Owner.Id] = blip;
                updated = true;
            }
        }

        if (updated)
            tacticalMap.MapDirty = true;
    }

    public override void UpdateUserData(Entity<TacticalMapUserComponent> user, TacticalMapComponent map)
    {
        var lines = EnsureComp<TacticalMapLinesComponent>(user);
        var labels = EnsureComp<TacticalMapLabelsComponent>(user);

        RefreshUserVisibleLayers(user);
        var visibleLayers = GetActiveLayers(user.Comp.VisibleLayers, user.Comp.ActiveLayer);
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

        user.Comp.Blips = blips;

        var combinedLines = new List<TacticalMapLine>();
        var combinedLabels = new Dictionary<Vector2i, string>();

        foreach (var layer in visibleLayers)
        {
            if (!map.Layers.TryGetValue(layer, out var layerData))
                continue;

            combinedLines.AddRange(layerData.Lines);
            foreach (var (pos, text) in layerData.Labels)
            {
                combinedLabels[pos] = text;
            }
        }

        lines.Lines = combinedLines;
        labels.Labels = combinedLabels;

        Dirty(user);
        Dirty(user, lines);
        Dirty(user, labels);
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

    private void UpdateCanvas(Entity<TacticalMapComponent> map, List<TacticalMapLine> lines, Dictionary<Vector2i, string> labels, ProtoId<TacticalMapLayerPrototype> layer, EntityUid user, SoundSpecifier? sound = null)
    {
        var layerData = EnsureLayer(map.Comp, layer);
        map.Comp.MapDirty = true;

        layerData.Lines = lines;
        layerData.Labels = new Dictionary<Vector2i, string>(labels);
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
        var visibleLayers = GetActiveLayers(computer.Comp.VisibleLayers, computer.Comp.ActiveLayer);
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
        var combinedLabels = new Dictionary<Vector2i, string>();

        foreach (var layer in visibleLayers)
        {
            if (!map.Layers.TryGetValue(layer, out var layerData))
                continue;

            combinedLines.AddRange(layerData.Lines);
            foreach (var (pos, text) in layerData.Labels)
            {
                combinedLabels[pos] = text;
            }
        }

        lines.Lines = combinedLines;
        labels.Labels = combinedLabels;

        Dirty(computer, lines);
        Dirty(computer, labels);
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
        }
    }
}
