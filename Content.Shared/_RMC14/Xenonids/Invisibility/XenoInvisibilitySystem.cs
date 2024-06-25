using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Actions;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Invisibility;

public sealed class XenoInvisibilitySystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoTurnInvisibleComponent, XenoTurnInvisibleActionEvent>(OnXenoTurnInvisibleAction);

        SubscribeLocalEvent<XenoActiveInvisibleComponent, ComponentRemove>(OnXenoActiveInvisibleRemove);
        SubscribeLocalEvent<XenoActiveInvisibleComponent, MeleeAttackEvent>(OnXenoActiveInvisibleMeleeAttack);
        SubscribeLocalEvent<XenoActiveInvisibleComponent, XenoLeapHitEvent>(OnXenoActiveInvisibleLeapHit);
        SubscribeLocalEvent<XenoActiveInvisibleComponent, RefreshMovementSpeedModifiersEvent>(OnXenoActiveInvisibleRefreshSpeed);
    }

    private void OnXenoTurnInvisibleAction(Entity<XenoTurnInvisibleComponent> xeno, ref XenoTurnInvisibleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (HasComp<XenoActiveInvisibleComponent>(xeno))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-invisibility-already-invisible"), xeno, xeno);
            return;
        }

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        var active = EnsureComp<XenoActiveInvisibleComponent>(xeno);
        active.ExpiresAt = _timing.CurTime + xeno.Comp.Duration;
        active.Cooldown = xeno.Comp.Cooldown;
        active.SpeedMultiplier = xeno.Comp.SpeedMultiplier;

        _movementSpeed.RefreshMovementSpeedModifiers(xeno);
    }

    private void OnXenoActiveInvisibleRemove(Entity<XenoActiveInvisibleComponent> xeno, ref ComponentRemove args)
    {
        if (!TerminatingOrDeleted(xeno))
            _movementSpeed.RefreshMovementSpeedModifiers(xeno);
    }

    private void OnXenoActiveInvisibleMeleeAttack(Entity<XenoActiveInvisibleComponent> xeno, ref MeleeAttackEvent args)
    {
        RemCompDeferred<XenoActiveInvisibleComponent>(xeno);
        OnRemoveInvisibility(xeno);
    }

    private void OnXenoActiveInvisibleLeapHit(Entity<XenoActiveInvisibleComponent> xeno, ref XenoLeapHitEvent args)
    {
        RemCompDeferred<XenoActiveInvisibleComponent>(xeno);
        OnRemoveInvisibility(xeno);
    }

    private void OnXenoActiveInvisibleRefreshSpeed(Entity<XenoActiveInvisibleComponent> xeno, ref RefreshMovementSpeedModifiersEvent args)
    {
        var multiplier = xeno.Comp.SpeedMultiplier.Float();
        args.ModifySpeed(multiplier, multiplier);
    }

    private void OnRemoveInvisibility(Entity<XenoActiveInvisibleComponent> xeno)
    {
        foreach (var (actionId, actionComp) in _actions.GetActions(xeno))
        {
            if (actionComp.BaseEvent is XenoTurnInvisibleActionEvent)
                _actions.SetIfBiggerCooldown(actionId, xeno.Comp.Cooldown);
        }

        _movementSpeed.RefreshMovementSpeedModifiers(xeno);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var activeQuery = EntityQueryEnumerator<XenoActiveInvisibleComponent>();
        while (activeQuery.MoveNext(out var uid, out var active))
        {
            if (active.ExpiresAt > time)
                continue;

            RemCompDeferred<XenoActiveInvisibleComponent>(uid);
            OnRemoveInvisibility((uid, active));

            if (!active.DidPopup)
            {
                _popup.PopupClient(Loc.GetString("cm-xeno-invisibility-expire"), uid, uid, PopupType.SmallCaution);
                active.DidPopup = true;
                Dirty(uid, active);
            }
        }
    }
}
