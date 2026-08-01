using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Overwatch;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.TacticalMap;

public abstract class SharedTacticalMapSystem : EntitySystem
{
    private static readonly ProtoId<TacticalMapLayerPrototype> GlobalMarineLayer = "Marines";
    private static readonly ProtoId<TacticalMapLayerPrototype> GlobalXenoLayer = "Xenos";

    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly TacticalMapLayerAccessSystem _layerAccess = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private EntityQuery<SquadTeamComponent> _squadTeamQuery;
    private readonly Dictionary<string, ProtoId<TacticalMapLayerPrototype>> _squadLayerMap = new();

    public int LineLimit { get; private set; }

    public override void Initialize()
    {
        _squadTeamQuery = GetEntityQuery<SquadTeamComponent>();

        SubscribeLocalEvent<TacticalMapUserComponent, OpenTacticalMapActionEvent>(OnUserOpenAction);
        SubscribeLocalEvent<TacticalMapUserComponent, OpenTacMapAlertEvent>(OnUserOpenAlert);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        Subs.CVar(_config, RMCCVars.RMCTacticalMapLineLimit, v => LineLimit = v, true);

        RebuildSquadLayerMap();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<TacticalMapLayerPrototype>())
            RebuildSquadLayerMap();
    }

    private void RebuildSquadLayerMap()
    {
        // cache prototype squad ids for fast squad layer lookup.
        _squadLayerMap.Clear();
        foreach (var layer in _prototypes.EnumeratePrototypes<TacticalMapLayerPrototype>())
        {
            if (layer.SquadId == null)
                continue;

            _squadLayerMap[layer.SquadId.Value.Id] = layer.ID;
        }
    }

    private void OnUserOpenAction(Entity<TacticalMapUserComponent> ent, ref OpenTacticalMapActionEvent args)
    {
        if (TryResolveUserMap(ent, out var map))
            UpdateUserData(ent, map);

        ToggleMapUI(ent);
    }

    private void OnUserOpenAlert(Entity<TacticalMapUserComponent> ent, ref OpenTacMapAlertEvent args)
    {
        if (TryResolveUserMap(ent, out var map))
            UpdateUserData(ent, map);

        ToggleMapUI(ent);
    }

    protected virtual bool TryResolveUserMap(Entity<TacticalMapUserComponent> user, out Entity<TacticalMapComponent> map)
    {
        return TryGetTacticalMap(user.Comp.Map, out map);
    }

    protected virtual bool TryResolveComputerMap(Entity<TacticalMapComputerComponent> computer, out Entity<TacticalMapComponent> map)
    {
        return TryGetTacticalMap(computer.Comp.Map, out map);
    }

    protected IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> GetVisibleLayers(IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> layers)
    {
        return layers;
    }

    protected IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> GetActiveLayers(
        IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> layers,
        ProtoId<TacticalMapLayerPrototype>? activeLayer)
    {
        var visible = GetVisibleLayers(layers);
        if (activeLayer == null)
            return visible;
        return new List<ProtoId<TacticalMapLayerPrototype>> { activeLayer.Value };
    }

    protected HashSet<ProtoId<TacticalMapLayerPrototype>> ApplyLayerVisibilityRules(
        EntityUid? viewer,
        IEnumerable<ProtoId<TacticalMapLayerPrototype>> layers)
    {
        // other systems can remove layers before state is sent
        var visible = new HashSet<ProtoId<TacticalMapLayerPrototype>>(layers);
        var ev = new TacticalMapModifyVisibleLayersEvent(viewer, visible);
        RaiseLocalEvent(ref ev);
        return visible;
    }

    protected Dictionary<NetEntity, TacticalMapBlip> ToNetworkBlips(Dictionary<int, TacticalMapBlip> blips)
    {
        var netBlips = new Dictionary<NetEntity, TacticalMapBlip>(blips.Count);
        foreach (var (entityId, blip) in blips)
        {
            var uid = new EntityUid(entityId);
            if (TerminatingOrDeleted(uid))
                continue;

            netBlips[GetNetEntity(uid)] = blip;
        }

        return netBlips;
    }

    private IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> GetDefaultLayers()
    {
        var defaults = new List<ProtoId<TacticalMapLayerPrototype>>();
        foreach (var layer in _prototypes.EnumeratePrototypes<TacticalMapLayerPrototype>())
        {
            if (layer.DefaultVisible)
                defaults.Add(layer.ID);
        }

        defaults.Sort(CompareLayerOrder);
        return defaults;
    }

    protected int CompareLayerOrder(ProtoId<TacticalMapLayerPrototype> a, ProtoId<TacticalMapLayerPrototype> b)
    {
        var orderA = _prototypes.TryIndex(a, out var layerA) ? layerA.SortOrder : 0;
        var orderB = _prototypes.TryIndex(b, out var layerB) ? layerB.SortOrder : 0;
        var compare = orderA.CompareTo(orderB);
        return compare != 0 ? compare : string.CompareOrdinal(a.Id, b.Id);
    }

    protected void RefreshUserVisibleLayers(Entity<TacticalMapUserComponent> user)
    {
        var baseLayers = EnsureBaseLayers(user);
        // players use explicit layer sets, not global defaults.
        var options = BuildLayerOptions(user.Owner, baseLayers, includeLayerAccess: true, includeAllSquads: false, allowDefaultVisible: false);
        ApplyLayerOptions(user, options);
        ApplyVisibleLayerSelection(user, options, baseLayers);
    }

    protected void RefreshComputerVisibleLayers(Entity<TacticalMapComputerComponent> computer)
    {
        var baseLayers = EnsureBaseLayers(computer);
        var allowedSquadGroups = GetAllowedSquadGroups(computer.Owner);
        // computers can expose command-wide layers.
        var options = BuildLayerOptions(computer.Owner, baseLayers, includeLayerAccess: false, includeAllSquads: true, allowedSquadGroups: allowedSquadGroups);
        ApplyLayerOptions(computer, options);
        ApplyVisibleLayerSelection(computer, options, baseLayers);
        EnsureOverwatchDrawLayer(computer);
    }

    protected List<ProtoId<TacticalMapLayerPrototype>> ResolveLayerSets(IReadOnlyList<ProtoId<TacticalMapLayerSetPrototype>> sets)
    {
        var layers = new List<ProtoId<TacticalMapLayerPrototype>>();
        var seen = new HashSet<ProtoId<TacticalMapLayerPrototype>>();
        foreach (var setId in sets)
        {
            if (!_prototypes.TryIndex(setId, out var setProto))
                continue;

            // preserve yaml order while removing duplicates.
            foreach (var layer in setProto.Layers)
            {
                if (seen.Add(layer))
                    layers.Add(layer);
            }
        }

        return layers;
    }

    protected IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> EnsureBaseLayers(Entity<TacticalMapUserComponent> user)
    {
        if (user.Comp.BaseLayers.Count == 0 && user.Comp.LayerSets.Count > 0)
        {
            user.Comp.BaseLayers = ResolveLayerSets(user.Comp.LayerSets);
            Dirty(user);
        }

        if (user.Comp.BaseLayers.Count == 0 && user.Comp.VisibleLayers.Count > 0)
        {
            user.Comp.BaseLayers = new List<ProtoId<TacticalMapLayerPrototype>>(user.Comp.VisibleLayers);
            Dirty(user);
        }

        return user.Comp.BaseLayers;
    }

    protected IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> EnsureBaseLayers(Entity<TacticalMapComputerComponent> computer)
    {
        if (computer.Comp.BaseLayers.Count == 0 && computer.Comp.LayerSets.Count > 0)
        {
            computer.Comp.BaseLayers = ResolveLayerSets(computer.Comp.LayerSets);
            Dirty(computer);
        }

        if (computer.Comp.BaseLayers.Count == 0 && computer.Comp.VisibleLayers.Count > 0)
        {
            computer.Comp.BaseLayers = new List<ProtoId<TacticalMapLayerPrototype>>(computer.Comp.VisibleLayers);
            Dirty(computer);
        }

        return computer.Comp.BaseLayers;
    }

    protected List<ProtoId<TacticalMapLayerPrototype>> BuildLayerOptions(
        EntityUid? viewer,
        IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> baseLayers,
        bool includeLayerAccess,
        bool includeAllSquads,
        bool allowDefaultVisible = true,
        IReadOnlyList<string>? allowedSquadGroups = null)
    {
        var baseOrder = new List<ProtoId<TacticalMapLayerPrototype>>(baseLayers);
        if (allowDefaultVisible)
            AddDefaultVisibleLayers(baseOrder);

        // options are base layers plus temporary access grants.
        var layers = new HashSet<ProtoId<TacticalMapLayerPrototype>>(baseOrder);
        var accessLayers = new HashSet<ProtoId<TacticalMapLayerPrototype>>();

        if (includeLayerAccess && viewer != null && _layerAccess.TryGetLayers(viewer.Value, accessLayers))
        {
            foreach (var layer in accessLayers)
            {
                layers.Add(layer);
            }
        }

        if (includeAllSquads)
            AddAllSquadLayers(layers);
        else if (viewer != null)
            AddViewerSquadLayer(viewer.Value, layers);

        FilterSquadGroups(layers, allowedSquadGroups);
        ApplyLayerVisibilityRules(viewer, layers, accessLayers, includeAllSquads);
        FilterEmptySquadLayers(layers);

        return OrderLayers(layers, baseOrder);
    }

    protected void AddDefaultVisibleLayers(List<ProtoId<TacticalMapLayerPrototype>> baseOrder)
    {
        foreach (var layer in _prototypes.EnumeratePrototypes<TacticalMapLayerPrototype>())
        {
            if (layer.DefaultVisible && !baseOrder.Contains(layer.ID))
                baseOrder.Add(layer.ID);
        }
    }

    private void ApplyLayerVisibilityRules(
        EntityUid? viewer,
        HashSet<ProtoId<TacticalMapLayerPrototype>> layers,
        HashSet<ProtoId<TacticalMapLayerPrototype>> accessLayers,
        bool includeAllSquads)
    {
        if (layers.Count == 0)
            return;

        // yaml visibility rules are enforced after all candidate layers merge.
        var toRemove = new List<ProtoId<TacticalMapLayerPrototype>>();
        foreach (var layerId in layers)
        {
            if (!_prototypes.TryIndex(layerId, out var layer))
                continue;

            switch (layer.Visibility)
            {
                case TacticalMapLayerVisibility.None:
                    toRemove.Add(layerId);
                    break;
                case TacticalMapLayerVisibility.LayerAccess:
                    if (!accessLayers.Contains(layerId))
                        toRemove.Add(layerId);
                    break;
                case TacticalMapLayerVisibility.SquadMembers:
                    if (!includeAllSquads && !IsViewerInSquadLayer(viewer, layer))
                        toRemove.Add(layerId);
                    break;
                case TacticalMapLayerVisibility.SquadOrAccess:
                    if (!accessLayers.Contains(layerId) && !includeAllSquads && !IsViewerInSquadLayer(viewer, layer))
                        toRemove.Add(layerId);
                    break;
            }
        }

        foreach (var remove in toRemove)
        {
            layers.Remove(remove);
        }
    }

    protected IReadOnlyList<string>? GetAllowedSquadGroups(EntityUid computerOwner)
    {
        if (!TryComp(computerOwner, out TacticalMapComputerComponent? computer))
            return null;

        if (computer.AllowedSquadGroups.Count == 0)
            return null;

        return computer.AllowedSquadGroups;
    }

    protected void FilterSquadGroups(HashSet<ProtoId<TacticalMapLayerPrototype>> layers, IReadOnlyList<string>? allowedSquadGroups)
    {
        if (allowedSquadGroups == null || allowedSquadGroups.Count == 0)
            return;

        var toRemove = new List<ProtoId<TacticalMapLayerPrototype>>();
        foreach (var layerId in layers)
        {
            if (!TryGetSquadGroup(layerId, out var group))
                continue;

            if (group == null || !ContainsGroup(allowedSquadGroups, group))
                toRemove.Add(layerId);
        }

        foreach (var remove in toRemove)
        {
            layers.Remove(remove);
        }
    }

    private static bool ContainsGroup(IReadOnlyList<string> allowedSquadGroups, string group)
    {
        foreach (var allowed in allowedSquadGroups)
        {
            if (string.Equals(allowed, group, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    protected void FilterEmptySquadLayers(HashSet<ProtoId<TacticalMapLayerPrototype>> layers)
    {
        if (layers.Count == 0)
            return;

        // empty squads should not appear as selectable layers.
        var toRemove = new List<ProtoId<TacticalMapLayerPrototype>>();
        foreach (var layerId in layers)
        {
            if (!_prototypes.TryIndex(layerId, out var layer) || layer.SquadId == null)
                continue;

            if (!_squad.TryGetSquad(layer.SquadId.Value, out var squad))
            {
                toRemove.Add(layerId);
                continue;
            }

            if (_squad.GetSquadMembersAlive(squad) <= 0)
                toRemove.Add(layerId);
        }

        foreach (var remove in toRemove)
        {
            layers.Remove(remove);
        }
    }

    protected bool TryGetSquadGroup(ProtoId<TacticalMapLayerPrototype> layerId, out string? group)
    {
        group = null;
        if (!_prototypes.TryIndex(layerId, out var layer) || layer.SquadId == null)
            return false;

        if (!layer.SquadId.Value.TryGet(out var squad, _prototypes, _compFactory))
            return false;

        group = squad.Group;
        return true;
    }

    protected bool IsViewerInSquadLayer(EntityUid? viewer, TacticalMapLayerPrototype layer)
    {
        if (viewer == null || layer.SquadId == null)
            return false;

        if (!_squad.TryGetMemberSquad(viewer.Value, out var squad))
            return false;

        var squadId = MetaData(squad.Owner).EntityPrototype?.ID;
        return squadId != null && squadId == layer.SquadId.Value.Id;
    }

    protected bool TryGetSquadLayer(EntityUid squadEntity, out ProtoId<TacticalMapLayerPrototype> layer)
    {
        var prototypeId = MetaData(squadEntity).EntityPrototype?.ID;
        if (prototypeId != null && _squadLayerMap.TryGetValue(prototypeId, out layer))
            return true;

        if (_squadTeamQuery.TryComp(squadEntity, out var squadComp) && squadComp.TacticalMapLayer != null)
        {
            layer = squadComp.TacticalMapLayer.Value;
            return true;
        }

        layer = default;
        return false;
    }

    protected void AddViewerSquadLayer(EntityUid viewer, HashSet<ProtoId<TacticalMapLayerPrototype>> layers)
    {
        if (TryGetViewerSquadLayer(viewer, out var layer))
            layers.Add(layer);
    }

    protected bool TryGetViewerSquadLayer(EntityUid viewer, out ProtoId<TacticalMapLayerPrototype> layer)
    {
        if (!_squad.TryGetMemberSquad(viewer, out var squad))
        {
            layer = default;
            return false;
        }

        return TryGetSquadLayer(squad.Owner, out layer);
    }

    protected void AddAllSquadLayers(HashSet<ProtoId<TacticalMapLayerPrototype>> layers)
    {
        foreach (var layer in GetAllSquadLayers())
        {
            layers.Add(layer);
        }
    }

    protected HashSet<ProtoId<TacticalMapLayerPrototype>> GetAllSquadLayers()
    {
        var layers = new HashSet<ProtoId<TacticalMapLayerPrototype>>();
        foreach (var layer in _prototypes.EnumeratePrototypes<TacticalMapLayerPrototype>())
        {
            if (layer.Kind != TacticalMapLayerKind.Squad &&
                layer.Visibility != TacticalMapLayerVisibility.SquadMembers &&
                layer.Visibility != TacticalMapLayerVisibility.SquadOrAccess &&
                layer.SquadId == null)
            {
                continue;
            }

            layers.Add(layer.ID);
        }

        return layers;
    }

    protected void EnsureOverwatchDrawLayer(Entity<TacticalMapComputerComponent> computer)
    {
        // overwatch consoles draw on their assigned squad layer.
        if (HasComp<MarineCommunicationsComputerComponent>(computer.Owner))
            return;

        if (!TryComp(computer.Owner, out OverwatchConsoleComponent? console))
            return;

        if (console.Squad is not { } squadNet ||
            !TryGetEntity(squadNet, out var squadEnt) ||
            !TryGetSquadLayer(squadEnt.Value, out var layer))
        {
            return;
        }

        if (!computer.Comp.VisibleLayers.Contains(layer))
        {
            computer.Comp.VisibleLayers.Add(layer);
            ApplyVisibleLayerSelection(computer, computer.Comp.LayerOptions, EnsureBaseLayers(computer));
        }

        if (computer.Comp.ActiveLayer != layer)
        {
            computer.Comp.ActiveLayer = layer;
        }

        Dirty(computer);
    }

    protected List<ProtoId<TacticalMapLayerPrototype>> OrderLayers(
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

    protected void ApplyLayerOptions(Entity<TacticalMapUserComponent> user, List<ProtoId<TacticalMapLayerPrototype>> options)
    {
        if (!options.SequenceEqual(user.Comp.LayerOptions))
        {
            user.Comp.LayerOptions = options;
            Dirty(user);
        }
    }

    protected void ApplyLayerOptions(Entity<TacticalMapComputerComponent> computer, List<ProtoId<TacticalMapLayerPrototype>> options)
    {
        if (!options.SequenceEqual(computer.Comp.LayerOptions))
        {
            computer.Comp.LayerOptions = options;
            Dirty(computer);
        }
    }

    protected void ApplyVisibleLayerSelection(
        Entity<TacticalMapUserComponent> user,
        IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> options,
        IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> baseLayers)
    {
        // saved selections are trimmed back to layers still allowed now.
        var usingDefaults = user.Comp.VisibleLayers.Count == 0;
        var selected = usingDefaults
            ? (baseLayers.Count > 0
                ? new List<ProtoId<TacticalMapLayerPrototype>>(baseLayers)
                : new List<ProtoId<TacticalMapLayerPrototype>>(options))
            : new List<ProtoId<TacticalMapLayerPrototype>>(user.Comp.VisibleLayers);

        if (usingDefaults &&
            TryGetViewerSquadLayer(user.Owner, out var squadLayer) &&
            options.Contains(squadLayer) &&
            !selected.Contains(squadLayer))
        {
            selected.Add(squadLayer);
        }

        selected = selected.Distinct().Where(options.Contains).ToList();
        if (selected.Count == 0 && options.Count > 0)
            selected.Add(options[0]);

        selected = selected.Where(options.Contains).ToList();

        var ordered = OrderLayers(new HashSet<ProtoId<TacticalMapLayerPrototype>>(selected), options);
        if (!ordered.SequenceEqual(user.Comp.VisibleLayers))
        {
            user.Comp.VisibleLayers = ordered;
            Dirty(user);
        }

        EnsureActiveLayer(user, options);
    }

    protected void ApplyVisibleLayerSelection(
        Entity<TacticalMapComputerComponent> computer,
        IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> options,
        IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> baseLayers)
    {
        // saved selections are trimmed back to layers still allowed now.
        var selected = computer.Comp.VisibleLayers.Count == 0
            ? (baseLayers.Count > 0
                ? new List<ProtoId<TacticalMapLayerPrototype>>(baseLayers)
                : new List<ProtoId<TacticalMapLayerPrototype>>(options))
            : new List<ProtoId<TacticalMapLayerPrototype>>(computer.Comp.VisibleLayers);

        selected = selected.Distinct().Where(options.Contains).ToList();
        if (selected.Count == 0 && options.Count > 0)
            selected.Add(options[0]);

        selected = selected.Where(options.Contains).ToList();

        var ordered = OrderLayers(new HashSet<ProtoId<TacticalMapLayerPrototype>>(selected), options);
        if (!ordered.SequenceEqual(computer.Comp.VisibleLayers))
        {
            computer.Comp.VisibleLayers = ordered;
            Dirty(computer);
        }

        EnsureActiveLayer(computer, options);
    }

    protected void EnsureActiveLayer(Entity<TacticalMapUserComponent> user, IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> options)
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

    protected void EnsureActiveLayer(Entity<TacticalMapComputerComponent> computer, IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> options)
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

    protected bool TryGetSelectedLayer(string? layerId, out ProtoId<TacticalMapLayerPrototype>? selected)
    {
        selected = null;
        if (string.IsNullOrWhiteSpace(layerId))
            return true;

        if (!_prototypes.HasIndex<TacticalMapLayerPrototype>(layerId))
            return false;

        selected = layerId;
        return true;
    }

    protected bool TryGetDrawLayer(
        IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> visibleLayers,
        ProtoId<TacticalMapLayerPrototype>? activeLayer,
        out ProtoId<TacticalMapLayerPrototype> drawLayer)
    {
        // draw on the acive layer, or fall back to the first drawable visible layer.
        if (activeLayer != null && LayerAllowsDrawing(activeLayer.Value))
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

        foreach (var layer in fallback)
        {
            if (!LayerAllowsDrawing(layer))
                continue;

            drawLayer = layer;
            return true;
        }

        drawLayer = default;
        return false;
    }

    protected bool LayerAllowsDrawing(ProtoId<TacticalMapLayerPrototype> layer)
    {
        return !_prototypes.TryIndex(layer, out var proto) || proto.CanDraw;
    }

    protected bool LayerAllowsBlipUpdates(ProtoId<TacticalMapLayerPrototype> layer)
    {
        return _prototypes.TryIndex(layer, out var proto) && proto.CanUpdateBlips && proto.CanContainBlips;
    }

    protected bool LayerAllowsBlipStorage(ProtoId<TacticalMapLayerPrototype> layer)
    {
        return !_prototypes.TryIndex(layer, out var proto) || proto.CanContainBlips;
    }

    protected List<ProtoId<TacticalMapLayerPrototype>> GetEffectiveVisibleLayers(
        IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> visibleLayers)
    {
        return new List<ProtoId<TacticalMapLayerPrototype>>(GetVisibleLayers(visibleLayers));
    }

    protected string ResolveMapId(Entity<TacticalMapComponent> map)
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

    protected string ResolveMapDisplayName(Entity<TacticalMapComponent> map, string mapId)
    {
        if (!string.IsNullOrWhiteSpace(map.Comp.DisplayName))
            return map.Comp.DisplayName;

        var meta = MetaData(map.Owner);
        if (!string.IsNullOrWhiteSpace(meta.EntityName))
            return meta.EntityName;

        return mapId;
    }

    public bool TryGetTacticalMap(out Entity<TacticalMapComponent> map)
    {
        return TryGetTacticalMap(null, out map);
    }

    public bool TryGetTacticalMap(EntityUid? mapId, out Entity<TacticalMapComponent> map)
    {
        // callers may pass a saved map, otherwise use the first tacmap
        if (mapId != null && TryComp(mapId.Value, out TacticalMapComponent? mapComp))
        {
            map = (mapId.Value, mapComp);
            return true;
        }

        var query = EntityQueryEnumerator<TacticalMapComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            map = (uid, comp);
            return true;
        }

        map = default;
        return false;
    }

    protected void UpdateMapData(Entity<TacticalMapComputerComponent> computer)
    {
        if (!TryResolveComputerMap(computer, out var map))
            return;

        UpdateMapData(computer, map);
    }

    protected virtual void UpdateMapData(Entity<TacticalMapComputerComponent> computer, TacticalMapComponent map)
    {
        // shared fallback combines visible layers for simple map users
        var layers = ApplyLayerVisibilityRules(computer.Owner, GetVisibleLayers(computer.Comp.VisibleLayers));

        var blips = new Dictionary<int, TacticalMapBlip>();
        foreach (var layer in layers)
        {
            if (!map.Layers.TryGetValue(layer, out var data))
                continue;

            foreach (var (id, blip) in data.Blips)
            {
                blips.TryAdd(id, blip);
            }
        }

        computer.Comp.Blips = ToNetworkBlips(blips);

        Dirty(computer);

        var lines = EnsureComp<TacticalMapLinesComponent>(computer);
        var labels = EnsureComp<TacticalMapLabelsComponent>(computer);

        var visibleLayers = GetVisibleLayers(computer.Comp.VisibleLayers);
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
        Dirty(computer, lines);
        Dirty(computer, labels);
    }

    public virtual void OpenComputerMap(Entity<TacticalMapComputerComponent?> computer, EntityUid user)
    {
        if (!Resolve(computer, ref computer.Comp, false))
            return;

        _ui.TryOpenUi(computer.Owner, TacticalMapComputerUi.Key, user);
        UpdateMapData((computer, computer.Comp));
    }

    public virtual void UpdateUserData(Entity<TacticalMapUserComponent> user, TacticalMapComponent map)
    {
    }

    public virtual void SetComputerDrawLayerFromSquad(EntityUid computer, EntityUid squad)
    {
    }

    private void ToggleMapUI(Entity<TacticalMapUserComponent> user)
    {
        // actions & alerts both use same path
        if (_ui.IsUiOpen(user.Owner, TacticalMapUserUi.Key, user))
        {
            _ui.CloseUi(user.Owner, TacticalMapUserUi.Key, user);
            return;
        }

        _ui.TryOpenUi(user.Owner, TacticalMapUserUi.Key, user);
    }
}
