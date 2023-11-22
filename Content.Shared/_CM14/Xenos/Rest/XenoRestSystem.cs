using Content.Shared._CM14.Xenos.Crest;
using Content.Shared._CM14.Xenos.Fortify;
using Content.Shared._CM14.Xenos.Headbutt;
using Content.Shared._CM14.Xenos.Sweep;
using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;

namespace Content.Shared._CM14.Xenos.Rest;

public sealed class XenoRestSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
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

        if (HasComp<XenoRestingComponent>(xeno))
        {
            RemComp<XenoRestingComponent>(xeno);
            _appearance.SetData(xeno, XenoVisualLayers.Base, XenoRestState.NotResting);
        }
        else
        {
            AddComp<XenoRestingComponent>(xeno);
            _appearance.SetData(xeno, XenoVisualLayers.Base, XenoRestState.Resting);
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
