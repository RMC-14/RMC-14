using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Tracker.Xeno;
using Content.Shared._RMC14.Xenonids.Evolution;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.TacticalMap;

public abstract class SharedTacticalMapSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public int LineLimit { get; private set; }

    public override void Initialize()
    {
        SubscribeLocalEvent<TacticalMapUserComponent, OpenTacticalMapActionEvent>(OnUserOpenAction);
        SubscribeLocalEvent<TacticalMapUserComponent, OpenTacMapAlertEvent>(OnUserOpenAlert);

        Subs.CVar(_config, RMCCVars.RMCTacticalMapLineLimit, v => LineLimit = v, true);

        SubscribeLocalEvent<TacticalMapUserComponent, NewXenoEvolvedEvent>(OnXenoEvolve);
        SubscribeLocalEvent<TacticalMapUserComponent, XenoDevolvedEvent>(OnXenoDevolve);
        SubscribeLocalEvent<TacticalMapUserComponent, XenoEvolutionDoAfterEvent>(DoAfterXenoEvolve);
        SubscribeLocalEvent<TacticalMapUserComponent, AfterNewXenoEvolvedEvent>(OnAfterXenoDevolve);
    }

    private void OnUserOpenAction(Entity<TacticalMapUserComponent> ent, ref OpenTacticalMapActionEvent args)
    {
        if (TryGetTacticalMap(out var map))
            UpdateUserData(ent, map);

        ToggleMapUI(ent);
    }

    private void OnUserOpenAlert(Entity<TacticalMapUserComponent> ent, ref OpenTacMapAlertEvent args)
    {
        if (TryGetTacticalMap(out var map))
            UpdateUserData(ent, map);

        ToggleMapUI(ent);
    }

    public bool TryGetTacticalMap(out Entity<TacticalMapComponent> map)
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

    private void OnXenoEvolve(Entity<TacticalMapUserComponent> newXeno, ref NewXenoEvolvedEvent args)
    {
        MaintainTacmapOpen(newXeno, args.OldXeno);
    }


    private void OnXenoDevolve(Entity<TacticalMapUserComponent> newXeno, ref XenoDevolvedEvent args)
    {
        MaintainTacmapOpen(newXeno, args.OldXeno!);
    }

    // When a xeno begins to evolve or devolve, give the new entity a component
    // to mark it as needing to reopen the Tacmap
    private void MaintainTacmapOpen(Entity<TacticalMapUserComponent> newXeno, Entity<XenoEvolutionComponent> oldXeno)
    {
        if (_ui.IsUiOpen(oldXeno.Owner, TacticalMapUserUi.Key, oldXeno))
        {
            EnsureComp<ReopenTacticalMapComponent>(newXeno);
        }
    }

    private void DoAfterXenoEvolve(Entity<TacticalMapUserComponent> newXeno, ref XenoEvolutionDoAfterEvent args)
    {
        RestoreTacmapOpen(newXeno);
    }

    private void OnAfterXenoDevolve(Entity<TacticalMapUserComponent> newXeno, ref AfterNewXenoEvolvedEvent args)
    {
        RestoreTacmapOpen(newXeno);
    }

    // When a xeno evolution finishes evolving, look for a component marking
    // whether to reopen the Tacmap
    private void RestoreTacmapOpen(Entity<TacticalMapUserComponent> newXeno)
    {
        if (!TryComp<ReopenTacticalMapComponent>(newXeno.Owner, out _))
        {
            return;
        }

        _ui.TryOpenUi(newXeno.Owner, TacticalMapUserUi.Key, newXeno);

        RemComp<ReopenTacticalMapComponent>(newXeno.Owner);
    }
}
