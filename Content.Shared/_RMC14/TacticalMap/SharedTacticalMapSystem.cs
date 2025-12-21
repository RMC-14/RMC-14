using System.Collections.Generic;
using Content.Shared._RMC14.CCVar;
using Robust.Shared.Configuration;

namespace Content.Shared._RMC14.TacticalMap;

public abstract class SharedTacticalMapSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private static readonly List<TacticalMapLayer> DefaultLayers = new() { TacticalMapLayer.Marines };

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

    protected IReadOnlyList<TacticalMapLayer> GetVisibleLayers(IReadOnlyList<TacticalMapLayer> layers)
    {
        return layers.Count > 0 ? layers : DefaultLayers;
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
        var ev = new TacticalMapIncludeXenosEvent();
        RaiseLocalEvent(ref ev);
        var layers = new HashSet<TacticalMapLayer>(GetVisibleLayers(computer.Comp.VisibleLayers));
        if (ev.Include && layers.Contains(TacticalMapLayer.Marines))
            layers.Add(TacticalMapLayer.Xenos);

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
        lines.Lines = map.Layers.TryGetValue(TacticalMapLayer.Marines, out var marineLayer)
            ? marineLayer.Lines
            : new List<TacticalMapLine>();
        Dirty(computer, lines);
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
