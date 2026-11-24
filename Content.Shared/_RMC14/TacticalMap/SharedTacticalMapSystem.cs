using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Xenonids.Evolution;
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
        SubscribeLocalEvent<TacticalMapUserComponent, OpenTacMapAlertEvent>(OnUserOpenAlert);

        Subs.CVar(_config, RMCCVars.RMCTacticalMapLineLimit, v => LineLimit = v, true);

        SubscribeLocalEvent<TacticalMapUserComponent, NewXenoEvolvedEvent>(OnXenoEvolve);
        SubscribeLocalEvent<TacticalMapUserComponent, XenoDevolvedEvent>(OnXenoDevolve);
        SubscribeLocalEvent<ReopenTacticalMapComponent, AfterNewXenoEvolvedEvent>(OnAfterXenoEvolve);
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

    public bool IsXenoStructureOnUserMap(Entity<TacticalMapUserComponent> user, EntityUid structure)
    {
        if (!user.Comp.Xenos ||
            user.Comp.Map is not { } map ||
            !user.Comp.XenoStructureBlips.ContainsKey(structure.Id))
        {
            return false;
        }

        return Transform(structure).GridUid == map;
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

    public virtual bool HasValidPosition(EntityUid ent, ref Vector2i indices)
    {
        return false;
    }

    private void OnXenoEvolve(Entity<TacticalMapUserComponent> newXeno, ref NewXenoEvolvedEvent args)
    {
        MaintainTacmapOpen(newXeno, args.OldXeno);
    }

    private void OnXenoDevolve(Entity<TacticalMapUserComponent> newXeno, ref XenoDevolvedEvent args)
    {
        MaintainTacmapOpen(newXeno, args.OldXeno);
    }

    // When a xeno begins to evolve or devolve, give the new entity a component
    // to mark it as needing to reopen the Tacmap
    private void MaintainTacmapOpen(Entity<TacticalMapUserComponent> newXeno, EntityUid oldXeno)
    {
        if (_ui.IsUiOpen(oldXeno, TacticalMapUserUi.Key, oldXeno))
            EnsureComp<ReopenTacticalMapComponent>(newXeno);
    }

    // When a xeno with that component finishes evolving, reopen the Tacmap.
    private void OnAfterXenoEvolve(Entity<ReopenTacticalMapComponent> newXeno, ref AfterNewXenoEvolvedEvent args)
    {
        if (HasComp<TacticalMapUserComponent>(newXeno))
            _ui.TryOpenUi(newXeno.Owner, TacticalMapUserUi.Key, newXeno);
        RemComp<ReopenTacticalMapComponent>(newXeno);
    }
}
