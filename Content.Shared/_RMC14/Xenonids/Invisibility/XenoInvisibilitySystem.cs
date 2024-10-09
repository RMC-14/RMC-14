using Content.Shared._RMC14.Xenonids.Devour;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
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
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoTurnInvisibleComponent, XenoTurnInvisibleActionEvent>(OnXenoTurnInvisibleAction);

        SubscribeLocalEvent<XenoActiveInvisibleComponent, ComponentRemove>(OnXenoActiveInvisibleRemove);
        SubscribeLocalEvent<XenoActiveInvisibleComponent, MeleeHitEvent>(OnXenoActiveInvisibleMeleeHit);
        SubscribeLocalEvent<XenoActiveInvisibleComponent, DoAfterAttemptEvent<XenoDevourDoAfterEvent>>(OnXenoDevourDoAfterAttempt);
        SubscribeLocalEvent<XenoActiveInvisibleComponent, XenoLeapHitEvent>(OnXenoActiveInvisibleLeapHit);
        SubscribeLocalEvent<XenoActiveInvisibleComponent, RefreshMovementSpeedModifiersEvent>(OnXenoActiveInvisibleRefreshSpeed);
    }

    private void OnXenoTurnInvisibleAction(Entity<XenoTurnInvisibleComponent> xeno, ref XenoTurnInvisibleActionEvent args)
    {
        //if (args.Handled)
        //    return;

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        if (TryComp<XenoActiveInvisibleComponent>(xeno, out var oldActive)){
            RemCompDeferred<XenoActiveInvisibleComponent>(xeno);
            
            foreach (var (actionId, actionComp) in _actions.GetActions(xeno))
            {
                if (actionComp.BaseEvent is XenoTurnInvisibleActionEvent)
                {
                    _actions.SetCooldown(actionId, xeno.Comp.Cooldown);
                }
            }
        }
        else
        {
            var active = EnsureComp<XenoActiveInvisibleComponent>(xeno);
            active.ExpiresAt = _timing.CurTime + xeno.Comp.Duration;
            active.Cooldown = xeno.Comp.Cooldown;
            active.SpeedMultiplier = xeno.Comp.SpeedMultiplier; 

            _movementSpeed.RefreshMovementSpeedModifiers(xeno);
        }

        //args.Handled = true;
    }

    private void OnXenoActiveInvisibleRemove(Entity<XenoActiveInvisibleComponent> xeno, ref ComponentRemove args)
    {
        if (!TerminatingOrDeleted(xeno))
            _movementSpeed.RefreshMovementSpeedModifiers(xeno);
    }

    private void OnXenoActiveInvisibleMeleeHit(Entity<XenoActiveInvisibleComponent> xeno, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        foreach (var entity in args.HitEntities)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, entity))
                return;
        
            RemCompDeferred<XenoActiveInvisibleComponent>(xeno);
            OnRemoveInvisibility(xeno);

            break;
        }
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

    private void OnXenoDevourDoAfterAttempt(Entity<XenoActiveInvisibleComponent> xeno, ref DoAfterAttemptEvent<XenoDevourDoAfterEvent> args)
    {
        RemCompDeferred<XenoActiveInvisibleComponent>(xeno);
        OnRemoveInvisibility(xeno);
    }

    private void OnRemoveInvisibility(Entity<XenoActiveInvisibleComponent> xeno)
    {
        foreach (var (actionId, actionComp) in _actions.GetActions(xeno))
        {
            if (actionComp.BaseEvent is XenoTurnInvisibleActionEvent)
            {
                _actions.SetCooldown(actionId, xeno.Comp.Cooldown);
            }
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
