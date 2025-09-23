using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Evasion;
using Content.Shared._RMC14.Xenonids.Charge;
using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared._RMC14.Xenonids.Crest;
using Content.Shared._RMC14.Xenonids.Fling;
using Content.Shared._RMC14.Xenonids.Fortify;
using Content.Shared._RMC14.Xenonids.Gut;
using Content.Shared._RMC14.Xenonids.Headbutt;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared._RMC14.Xenonids.Lunge;
using Content.Shared._RMC14.Xenonids.Punch;
using Content.Shared._RMC14.Xenonids.Screech;
using Content.Shared._RMC14.Xenonids.Stomp;
using Content.Shared._RMC14.Xenonids.Sweep;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Rest;

public sealed class XenoRestSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoComponent, XenoRestActionEvent>(OnXenoRestAction);

        SubscribeLocalEvent<XenoRestingComponent, UpdateCanMoveEvent>(OnXenoRestingCanMove);
        SubscribeLocalEvent<XenoRestingComponent, AttackAttemptEvent>(OnXenoRestingMeleeHit);
        SubscribeLocalEvent<XenoRestingComponent, XenoSecreteStructureAttemptEvent>(OnXenoSecreteStructureAttempt);
        SubscribeLocalEvent<XenoRestingComponent, XenoHeadbuttAttemptEvent>(OnXenoRestingHeadbuttAttempt);
        SubscribeLocalEvent<XenoRestingComponent, XenoFortifyAttemptEvent>(OnXenoRestingFortifyAttempt);
        SubscribeLocalEvent<XenoRestingComponent, XenoTailSweepAttemptEvent>(OnXenoRestingTailSweepAttempt);
        SubscribeLocalEvent<XenoRestingComponent, XenoToggleCrestAttemptEvent>(OnXenoRestingToggleCrestAttempt);
        SubscribeLocalEvent<XenoRestingComponent, XenoLeapAttemptEvent>(OnXenoRestingLeapAttempt);
        SubscribeLocalEvent<XenoRestingComponent, XenoLungeAttemptEvent>(OnXenoRestingLungeAttempt);
        SubscribeLocalEvent<XenoRestingComponent, XenoPunchAttemptEvent>(OnXenoRestingPunchAttempt);
        SubscribeLocalEvent<XenoRestingComponent, XenoFlingAttemptEvent>(OnXenoRestingFlingAttempt);
        SubscribeLocalEvent<XenoRestingComponent, XenoChargeAttemptEvent>(OnXenoRestingChargettempt);
        SubscribeLocalEvent<XenoRestingComponent, XenoStompAttemptEvent>(OnXenoRestingStompAttempt);
        SubscribeLocalEvent<XenoRestingComponent, XenoGutAttemptEvent>(OnXenoRestingGutAttempt);
        SubscribeLocalEvent<XenoRestingComponent, XenoScreechAttemptEvent>(OnXenoRestingScreechAttempt);
        SubscribeLocalEvent<XenoRestingComponent, EvasionRefreshModifiersEvent>(OnXenoRestingEvasionRefresh);
        SubscribeLocalEvent<XenoRestingComponent, AttemptMobCollideEvent>(OnXenoRestingMobCollide);
        SubscribeLocalEvent<XenoRestingComponent, AttemptMobTargetCollideEvent>(OnXenoRestingMobTargetCollide);

        SubscribeLocalEvent<ActionBlockIfRestingComponent, RMCActionUseAttemptEvent>(OnXenoRestingActionUseAttempt);
    }

    private void OnXenoRestingActionUseAttempt(Entity<ActionBlockIfRestingComponent> ent, ref RMCActionUseAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var user = args.User;
        if (HasComp<XenoRestingComponent>(user))
        {
            args.Cancelled = true;
            _popup.PopupClient(Loc.GetString(ent.Comp.Popup), user, user, PopupType.SmallCaution);
        }
    }

    private void OnXenoRestingCanMove(Entity<XenoRestingComponent> xeno, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnXenoRestAction(Entity<XenoComponent> xeno, ref XenoRestActionEvent args)
    {
        if (_timing.ApplyingState)
            return;

        var attempt = new XenoRestAttemptEvent();
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        args.Handled = true;

        if (HasComp<XenoRestingComponent>(xeno))
        {
            RemComp<XenoRestingComponent>(xeno);
            _appearance.SetData(xeno, XenoVisualLayers.Base, XenoRestState.NotResting);
            _actions.SetToggled(args.Action.AsNullable(), false);
        }
        else
        {
            AddComp<XenoRestingComponent>(xeno);
            _appearance.SetData(xeno, XenoVisualLayers.Base, XenoRestState.Resting);
            _actions.SetToggled(args.Action.AsNullable(), true);
        }

        _actionBlocker.UpdateCanMove(xeno);

        var ev = new XenoRestEvent(HasComp<XenoRestingComponent>(xeno));
        RaiseLocalEvent(xeno, ref ev);
    }

    private void OnXenoRestingMeleeHit(Entity<XenoRestingComponent> xeno, ref AttackAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnXenoSecreteStructureAttempt(Entity<XenoRestingComponent> xeno, ref XenoSecreteStructureAttemptEvent args)
    {
        _popup.PopupClient(Loc.GetString("rmc-xeno-rest-cant-secrete"), xeno, xeno);
        args.Cancelled = true;
    }

    private void OnXenoRestingHeadbuttAttempt(Entity<XenoRestingComponent> xeno, ref XenoHeadbuttAttemptEvent args)
    {
        _popup.PopupClient(Loc.GetString("rmc-xeno-rest-cant-headbutt"), xeno, xeno);
        args.Cancelled = true;
    }

    private void OnXenoRestingFortifyAttempt(Entity<XenoRestingComponent> xeno, ref XenoFortifyAttemptEvent args)
    {
        _popup.PopupClient(Loc.GetString("rmc-xeno-rest-cant-fortify"), xeno, xeno);
        args.Cancelled = true;
    }

    private void OnXenoRestingTailSweepAttempt(Entity<XenoRestingComponent> xeno, ref XenoTailSweepAttemptEvent args)
    {
        _popup.PopupClient(Loc.GetString("rmc-xeno-rest-cant-tail-sweep"), xeno, xeno);
        args.Cancelled = true;
    }

    private void OnXenoRestingToggleCrestAttempt(Entity<XenoRestingComponent> xeno, ref XenoToggleCrestAttemptEvent args)
    {
        _popup.PopupClient(Loc.GetString("rmc-xeno-rest-cant-toggle-crest"), xeno, xeno);
        args.Cancelled = true;
    }

    private void OnXenoRestingLeapAttempt(Entity<XenoRestingComponent> xeno, ref XenoLeapAttemptEvent args)
    {
        _popup.PopupClient(Loc.GetString("rmc-xeno-rest-cant-leap"), xeno, xeno);
        args.Cancelled = true;
    }

    private void OnXenoRestingLungeAttempt(Entity<XenoRestingComponent> xeno, ref XenoLungeAttemptEvent args)
    {
        _popup.PopupClient(Loc.GetString("rmc-xeno-rest-cant-lunge"), xeno, xeno);
        args.Cancelled = true;
    }

    private void OnXenoRestingPunchAttempt(Entity<XenoRestingComponent> xeno, ref XenoPunchAttemptEvent args)
    {
        _popup.PopupClient(Loc.GetString("rmc-xeno-rest-cant-punch"), xeno, xeno);
        args.Cancelled = true;
    }

    private void OnXenoRestingFlingAttempt(Entity<XenoRestingComponent> xeno, ref XenoFlingAttemptEvent args)
    {
        _popup.PopupClient(Loc.GetString("rmc-xeno-rest-cant-fling"), xeno, xeno);
        args.Cancelled = true;
    }

    private void OnXenoRestingChargettempt(Entity<XenoRestingComponent> xeno, ref XenoChargeAttemptEvent args)
    {
        _popup.PopupClient(Loc.GetString("rmc-xeno-rest-cant-charge"), xeno, xeno);
        args.Cancelled = true;
    }

    private void OnXenoRestingStompAttempt(Entity<XenoRestingComponent> xeno, ref XenoStompAttemptEvent args)
    {
        _popup.PopupClient(Loc.GetString("rmc-xeno-rest-cant-stomp"), xeno, xeno);
        args.Cancelled = true;
    }

    private void OnXenoRestingGutAttempt(Entity<XenoRestingComponent> xeno, ref XenoGutAttemptEvent args)
    {
        _popup.PopupClient(Loc.GetString("rmc-xeno-rest-cant-gut"), xeno, xeno);
        args.Cancelled = true;
    }

    private void OnXenoRestingScreechAttempt(Entity<XenoRestingComponent> xeno, ref XenoScreechAttemptEvent args)
    {
        _popup.PopupClient(Loc.GetString("rmc-xeno-rest-cant-screech"), xeno, xeno);
        args.Cancelled = true;
    }

    private void OnXenoRestingEvasionRefresh(Entity<XenoRestingComponent> xeno, ref EvasionRefreshModifiersEvent args)
    {
        if (xeno.Owner != args.Entity.Owner)
            return;

        args.Evasion += (int) EvasionModifiers.Rest;
    }

    private void OnXenoRestingMobCollide(Entity<XenoRestingComponent> ent, ref AttemptMobCollideEvent args)
    {
        args.Cancelled = true;
    }

    private void OnXenoRestingMobTargetCollide(Entity<XenoRestingComponent> ent, ref AttemptMobTargetCollideEvent args)
    {
        args.Cancelled = true;
    }

    public bool IsResting(Entity<XenoRestingComponent?> ent)
    {
        return Resolve(ent, ref ent.Comp, false);
    }
}
