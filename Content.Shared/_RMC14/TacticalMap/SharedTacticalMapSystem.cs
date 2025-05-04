using Content.Shared._RMC14.CCVar;
using Robust.Shared.Configuration;

namespace Content.Shared._RMC14.TacticalMap;

public abstract class SharedTacticalMapSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public int LineLimit { get; private set; }

    public override void Initialize()
    {
        SubscribeLocalEvent<TacticalMapUserComponent, OpenTacticalMapActionEvent>(OnUserOpenAction);

        Subs.CVar(_config, RMCCVars.RMCTacticalMapLineLimit, v => LineLimit = v, true);
    }

    protected virtual void OnUserOpenAction(Entity<TacticalMapUserComponent> ent, ref OpenTacticalMapActionEvent args)
    {
        _ui.TryOpenUi(ent.Owner, TacticalMapUserUi.Key, ent);
    }

    protected bool TryGetTacticalMap(out Entity<TacticalMapComponent> map)
    {
        var query = EntityQueryEnumerator<TacticalMapComponent>();
        while (query.MoveNext(out var uid, out var mapComp))
        {
            map = (uid, mapComp);
            return true;
        }

        map = default;
        return false;
    }

    protected void UpdateMapData(Entity<TacticalMapComputerComponent> computer)
    {
        if (!TryGetTacticalMap(out var map))
            return;

        UpdateMapData(computer, map);
    }

    protected void UpdateMapData(Entity<TacticalMapComputerComponent> computer, TacticalMapComponent map)
    {
        var ev = new TacticalMapIncludeXenosEvent();
        RaiseLocalEvent(ref ev);
        if (ev.Include)
        {
            computer.Comp.Blips = new Dictionary<int, TacticalMapBlip>(map.MarineBlips);
            foreach (var blip in map.XenoBlips)
            {
                computer.Comp.Blips.TryAdd(blip.Key, blip.Value);
            }
        }
        else
        {
            computer.Comp.Blips = map.MarineBlips;
        }

        Dirty(computer);

        var lines = EnsureComp<TacticalMapLinesComponent>(computer);
        lines.MarineLines = map.MarineLines;
        Dirty(computer, lines);
    }

    public void OpenComputerMap(Entity<TacticalMapComputerComponent?> computer, EntityUid user)
    {
        if (!Resolve(computer, ref computer.Comp, false))
            return;

        _ui.TryOpenUi(computer.Owner, TacticalMapComputerUi.Key, user);
        UpdateMapData((computer, computer.Comp));
    }
}
