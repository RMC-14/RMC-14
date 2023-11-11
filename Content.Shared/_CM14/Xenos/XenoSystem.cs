using Content.Shared._CM14.Xenos.Hive;
using Content.Shared.Actions;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos;

public sealed class XenoSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoComponent, MapInitEvent>(OnXenoMapInit);
        SubscribeLocalEvent<XenoComponent, EntityUnpausedEvent>(OnXenoUnpaused);
    }

    private void OnXenoMapInit(Entity<XenoComponent> ent, ref MapInitEvent args)
    {
        foreach (var actionId in ent.Comp.ActionIds)
        {
            if (!ent.Comp.Actions.ContainsKey(actionId) &&
                _action.AddAction(ent, actionId) is { } newAction)
            {
                ent.Comp.Actions[actionId] = newAction;
            }
        }
    }

    private void OnXenoUnpaused(Entity<XenoComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.NextPlasmaRegenTime += args.PausedTime;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<XenoComponent>();
        var time = _timing.CurTime;

        while (query.MoveNext(out var uid, out var xeno))
        {
            if (time < xeno.NextPlasmaRegenTime)
                continue;

            xeno.Plasma += xeno.PlasmaRegen;
            xeno.NextPlasmaRegenTime = time + xeno.PlasmaRegenCooldown;
            Dirty(uid, xeno);
        }
    }

    public void MakeXeno(Entity<XenoComponent?> uid)
    {
        EnsureComp<XenoComponent>(uid);
    }

    public bool HasPlasma(Entity<XenoComponent> xeno, int plasma)
    {
        return xeno.Comp.Plasma >= plasma;
    }

    public bool TryRemovePlasmaPopup(Entity<XenoComponent> xeno, int plasma)
    {
        if (!HasPlasma(xeno, plasma))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-not-enough-plasma"), xeno, xeno);
            return false;
        }

        RemovePlasma(xeno, plasma);
        return true;
    }

    public void RemovePlasma(Entity<XenoComponent> xeno, int plasma)
    {
        xeno.Comp.Plasma = Math.Max(xeno.Comp.Plasma - plasma, 0);
        Dirty(xeno);
    }
}
