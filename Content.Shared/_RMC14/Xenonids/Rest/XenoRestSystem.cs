using Content.Shared._RMC14.Xenonids.Crest;
using Content.Shared._RMC14.Xenonids.Fortify;
using Content.Shared._RMC14.Xenonids.Headbutt;
using Content.Shared._RMC14.Xenonids.Sweep;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;

namespace Content.Shared._RMC14.Xenonids.Rest;

public sealed class XenoRestSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoComponent, XenoRestActionEvent>(OnXenoRestAction);

        SubscribeLocalEvent<XenoRestingComponent, UpdateCanMoveEvent>(OnXenoRestingCanMove);
        SubscribeLocalEvent<XenoRestingComponent, XenoHeadbuttAttemptEvent>(OnXenoRestingHeadbuttAttempt);
        SubscribeLocalEvent<XenoRestingComponent, XenoFortifyAttemptEvent>(OnXenoRestingFortifyAttempt);
        SubscribeLocalEvent<XenoRestingComponent, XenoTailSweepAttemptEvent>(OnXenoRestingTailSweepAttempt);
        SubscribeLocalEvent<XenoRestingComponent, XenoToggleCrestAttemptEvent>(OnXenoRestingToggleCrestAttempt);
    }

    private void OnXenoRestingCanMove(Entity<XenoRestingComponent> xeno, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnXenoRestAction(Entity<XenoComponent> xeno, ref XenoRestActionEvent args)
    {
        var ev = new XenoRestAttemptEvent();
        RaiseLocalEvent(xeno, ref ev);

        if (ev.Cancelled)
            return;

        args.Handled = true;

        if (HasComp<XenoRestingComponent>(xeno))
        {
            RemComp<XenoRestingComponent>(xeno);
            _appearance.SetData(xeno, XenoVisualLayers.Base, XenoRestState.NotResting);
            _actions.SetToggled(args.Action, false);
        }
        else
        {
            AddComp<XenoRestingComponent>(xeno);
            _appearance.SetData(xeno, XenoVisualLayers.Base, XenoRestState.Resting);
            _actions.SetToggled(args.Action, true);
        }

        _actionBlocker.UpdateCanMove(xeno);
    }

    private void OnXenoRestingHeadbuttAttempt(Entity<XenoRestingComponent> xeno, ref XenoHeadbuttAttemptEvent args)
    {
        _popup.PopupClient(Loc.GetString("cm-xeno-rest-cant-headbutt"), xeno, xeno);
        args.Cancelled = true;
    }

    private void OnXenoRestingFortifyAttempt(Entity<XenoRestingComponent> xeno, ref XenoFortifyAttemptEvent args)
    {
        _popup.PopupClient(Loc.GetString("cm-xeno-rest-cant-fortify"), xeno, xeno);
        args.Cancelled = true;
    }

    private void OnXenoRestingTailSweepAttempt(Entity<XenoRestingComponent> xeno, ref XenoTailSweepAttemptEvent args)
    {
        _popup.PopupClient(Loc.GetString("cm-xeno-rest-cant-tail-sweep"), xeno, xeno);
        args.Cancelled = true;
    }

    private void OnXenoRestingToggleCrestAttempt(Entity<XenoRestingComponent> xeno, ref XenoToggleCrestAttemptEvent args)
    {
        _popup.PopupClient(Loc.GetString("cm-xeno-rest-cant-toggle-crest"), xeno, xeno);
        args.Cancelled = true;
    }
}
