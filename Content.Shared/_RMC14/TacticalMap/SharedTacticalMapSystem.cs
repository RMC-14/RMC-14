using System.Collections.Generic;
using System.Linq;
using Content.Shared._RMC14.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.TacticalMap;

public abstract class SharedTacticalMapSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public int LineLimit { get; private set; }

    public override void Initialize()
    {
        SubscribeLocalEvent<TacticalMapUserComponent, OpenTacticalMapActionEvent>(OnUserOpenAction);
        SubscribeLocalEvent<TacticalMapUserComponent, OpenTacMapAlertEvent>(OnUserOpenAlert);

        Subs.CVar(_config, RMCCVars.RMCTacticalMapLineLimit, v => LineLimit = v, true);
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
        return layers.Count > 0 ? layers : GetDefaultLayers();
    }

    protected IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> GetActiveLayers(
        IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> layers,
        ProtoId<TacticalMapLayerPrototype>? activeLayer)
    {
        var visible = GetVisibleLayers(layers);
        if (activeLayer == null)
            return visible;

        if (visible.Contains(activeLayer.Value))
            return new List<ProtoId<TacticalMapLayerPrototype>> { activeLayer.Value };

        return visible;
    }

    protected HashSet<ProtoId<TacticalMapLayerPrototype>> ApplyLayerVisibilityRules(
        EntityUid? viewer,
        IEnumerable<ProtoId<TacticalMapLayerPrototype>> layers)
    {
        var visible = new HashSet<ProtoId<TacticalMapLayerPrototype>>(layers);
        var ev = new TacticalMapModifyVisibleLayersEvent(viewer, visible);
        RaiseLocalEvent(ref ev);
        return visible;
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

    private int CompareLayerOrder(ProtoId<TacticalMapLayerPrototype> a, ProtoId<TacticalMapLayerPrototype> b)
    {
        var orderA = _prototypes.TryIndex(a, out var layerA) ? layerA.SortOrder : 0;
        var orderB = _prototypes.TryIndex(b, out var layerB) ? layerB.SortOrder : 0;
        var compare = orderA.CompareTo(orderB);
        return compare != 0 ? compare : string.CompareOrdinal(a.Id, b.Id);
    }

    public bool TryGetTacticalMap(out Entity<TacticalMapComponent> map)
    {
        return TryGetTacticalMap(null, out map);
    }

    public bool TryGetTacticalMap(EntityUid? mapId, out Entity<TacticalMapComponent> map)
    {
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
        var layers = ApplyLayerVisibilityRules(computer.Owner, GetActiveLayers(computer.Comp.VisibleLayers, computer.Comp.ActiveLayer));

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

        computer.Comp.Blips = blips;

        Dirty(computer);

        var lines = EnsureComp<TacticalMapLinesComponent>(computer);
        var labels = EnsureComp<TacticalMapLabelsComponent>(computer);

        var visibleLayers = GetActiveLayers(computer.Comp.VisibleLayers, computer.Comp.ActiveLayer);
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

    private void ToggleMapUI(Entity<TacticalMapUserComponent> user)
    {
        if (_ui.IsUiOpen(user.Owner, TacticalMapUserUi.Key, user))
        {
            _ui.CloseUi(user.Owner, TacticalMapUserUi.Key, user);
            return;
        }

        _ui.TryOpenUi(user.Owner, TacticalMapUserUi.Key, user);
    }
}
