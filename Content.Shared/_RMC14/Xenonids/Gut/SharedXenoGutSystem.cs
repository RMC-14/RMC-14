using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Gibbing;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Jittering;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.Gut;

public sealed class SharedXenoGutSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly RMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly RMCGibSystem _rmcGib = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoGutComponent, XenoGutActionEvent>(OnXenoGutAction);
        SubscribeLocalEvent<XenoGutComponent, XenoGutDoAfterEvent>(OnXenoGutDoAfterEvent);
    }

    private void OnXenoGutAction(Entity<XenoGutComponent> xeno, ref XenoGutActionEvent args)
    {
        if (args.Target == xeno.Owner || HasComp<XenoComponent>(args.Target))
            return;

        if (args.Handled)
            return;

        var attempt = new XenoGutAttemptEvent();
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        var target = args.Target;

        if (!HasComp<BodyComponent>(target))
            return;

        if (!_xenoPlasma.HasPlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        args.Handled = true;

        var ev = new XenoGutDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.Delay, ev, xeno, target)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
        };

        var selfMsg = Loc.GetString("rmc-gut-start-self");
        var othersMsg = Loc.GetString("rmc-gut-start-others", ("user", xeno.Owner), ("target", args.Target));
        _popup.PopupPredicted(selfMsg, othersMsg, xeno.Owner, xeno.Owner, PopupType.LargeCaution);

        _doAfter.TryStartDoAfter(doAfter);
        _jitter.DoJitter(args.Target, xeno.Comp.Delay, true, 14f, 5f, true);
    }

    private void OnXenoGutDoAfterEvent(Entity<XenoGutComponent> xeno, ref XenoGutDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
        {
            if (args.Target is { } cancelledTarget)
                _statusEffects.TryRemoveStatusEffect(cancelledTarget, "Jitter");
            return;
        }

        if (target == xeno.Owner || HasComp<XenoComponent>(target))
            return;

        if (!TryComp<BodyComponent>(target, out var body))
            return;

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        args.Handled = true;
        if (_net.IsServer)
        {
            _rmcGib.ScatterInventoryItems(target);
            _bodySystem.GibBody(target, true, body);
            _audio.PlayPvs(xeno.Comp.Sound, xeno);
        }

        var selfMsg = Loc.GetString("rmc-gut-finish-self");
        var othersMsg = Loc.GetString("rmc-gut-finish-others", ("user", xeno.Owner), ("target", args.Target));
        _popup.PopupPredicted(selfMsg, othersMsg, xeno.Owner, xeno.Owner, PopupType.LargeCaution);

        foreach (var action in _rmcActions.GetActionsWithEvent<XenoGutActionEvent>(xeno))
        {
            _actions.SetIfBiggerCooldown(action.AsNullable(), xeno.Comp.Cooldown);
        }
    }
}
