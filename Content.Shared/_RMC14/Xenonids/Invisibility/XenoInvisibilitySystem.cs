using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids.Devour;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Invisibility;

public sealed class XenoInvisibilitySystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    private readonly HashSet<EntityUid> _contacts = new();

    private EntityQuery<MarineComponent> _marineQuery;
    private EntityQuery<MobCollisionComponent> _mobCollisionQuery;
    private EntityQuery<XenoComponent> _xenoQuery;

    public override void Initialize()
    {
        _marineQuery = GetEntityQuery<MarineComponent>();
        _mobCollisionQuery = GetEntityQuery<MobCollisionComponent>();
        _xenoQuery = GetEntityQuery<XenoComponent>();

        SubscribeLocalEvent<XenoTurnInvisibleComponent, XenoTurnInvisibleActionEvent>(OnXenoTurnInvisibleAction);

        SubscribeLocalEvent<XenoActiveInvisibleComponent, ComponentRemove>(OnXenoActiveInvisibleRemove);
        SubscribeLocalEvent<XenoActiveInvisibleComponent, MeleeHitEvent>(OnXenoActiveInvisibleMeleeHit);
        SubscribeLocalEvent<XenoActiveInvisibleComponent, DoAfterAttemptEvent<XenoDevourDoAfterEvent>>(OnXenoDevourDoAfterAttempt);
        SubscribeLocalEvent<XenoActiveInvisibleComponent, XenoLeapHitEvent>(OnXenoActiveInvisibleLeapHit);
        SubscribeLocalEvent<XenoActiveInvisibleComponent, RefreshMovementSpeedModifiersEvent>(OnXenoActiveInvisibleRefreshSpeed);
    }

    private void OnXenoTurnInvisibleAction(Entity<XenoTurnInvisibleComponent> xeno, ref XenoTurnInvisibleActionEvent args)
    {
        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        if (TryComp<XenoActiveInvisibleComponent>(xeno, out var invis))
        {
            var refundedCooldown = GetRefundedCooldown(xeno, invis, xeno.Comp.ManualRefundMultiplier);
            RemoveInvisibility((xeno, invis), refundedCooldown);
        }
        else
        {
            var active = EnsureComp<XenoActiveInvisibleComponent>(xeno);
            active.ExpiresAt = _timing.CurTime + xeno.Comp.Duration;
            active.FullCooldown = xeno.Comp.FullCooldown;
            active.SpeedMultiplier = xeno.Comp.SpeedMultiplier;

            //Half a second cooldown to prevent double clicks
            StartCooldown((xeno, active), xeno.Comp.ToggleLockoutTime, true);
            _movementSpeed.RefreshMovementSpeedModifiers(xeno);
        }
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

            RemoveInvisibility(xeno, xeno.Comp.FullCooldown);
            break;
        }
    }

    private void OnXenoDevourDoAfterAttempt(Entity<XenoActiveInvisibleComponent> xeno, ref DoAfterAttemptEvent<XenoDevourDoAfterEvent> args)
    {
        if(!TryComp<XenoTurnInvisibleComponent>(xeno, out var turnInvis))
        {
            RemoveInvisibility(xeno, xeno.Comp.FullCooldown);
            return;
        }

        var devourCooldown = GetRefundedCooldown((xeno, turnInvis), xeno.Comp, turnInvis.RevealedRefundMultiplier);
        RemoveInvisibility(xeno, devourCooldown);
    }

    private void OnXenoActiveInvisibleLeapHit(Entity<XenoActiveInvisibleComponent> xeno, ref XenoLeapHitEvent args)
    {
        RemoveInvisibility(xeno, xeno.Comp.FullCooldown);
    }

    private void OnXenoActiveInvisibleRefreshSpeed(Entity<XenoActiveInvisibleComponent> xeno, ref RefreshMovementSpeedModifiersEvent args)
    {
        var multiplier = xeno.Comp.SpeedMultiplier.Float();
        args.ModifySpeed(multiplier, multiplier);
    }

    private TimeSpan GetRefundedCooldown(Entity<XenoTurnInvisibleComponent> xeno, XenoActiveInvisibleComponent activeInvis, float refundMultiplier)
    {
        var timeRemaining = (activeInvis.ExpiresAt - _timing.CurTime) / xeno.Comp.Duration;
        var refundedCooldown = xeno.Comp.FullCooldown - timeRemaining * refundMultiplier * xeno.Comp.FullCooldown;

        return refundedCooldown;
    }

    private void StartCooldown(Entity<XenoActiveInvisibleComponent> xeno, TimeSpan cooldownTime, bool toggledStatus)
    {
        foreach (var action in _rmcActions.GetActionsWithEvent<XenoTurnInvisibleActionEvent>(xeno))
        {
            var actionEnt = action.AsNullable();
            _actions.SetCooldown(actionEnt, cooldownTime);
            _actions.SetToggled(actionEnt, toggledStatus);
        }
    }

    private void RemoveInvisibility(Entity<XenoActiveInvisibleComponent> xeno, TimeSpan cooldownTime)
    {
        RemCompDeferred<XenoActiveInvisibleComponent>(xeno);
        StartCooldown(xeno, cooldownTime, false);
        _movementSpeed.RefreshMovementSpeedModifiers(xeno);

        if (!xeno.Comp.DidPopup)
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-invisibility-expire"), xeno, xeno, PopupType.SmallCaution);
            xeno.Comp.DidPopup = true;
            Dirty(xeno);
        }
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var activeQuery = EntityQueryEnumerator<XenoActiveInvisibleComponent>();
        while (activeQuery.MoveNext(out var uid, out var active))
        {
            if (active.ExpiresAt <= time)
            {
                RemoveInvisibility((uid, active), active.FullCooldown);
                continue;
            }

            if (!_mobCollisionQuery.TryComp(uid, out var collision) ||
                !collision.Colliding)
            {
                continue;
            }

            _contacts.Clear();
            _physics.GetContactingEntities(uid, _contacts);

            var collidingEnemy = false;
            foreach (var contact in _contacts)
            {
                if (_hive.FromSameHive(uid, contact))
                    continue;

                if (!_marineQuery.HasComp(uid) && !_xenoQuery.HasComp(contact))
                    continue;

                collidingEnemy = true;
            }

            if (!collidingEnemy)
                continue;

            if (!active.DidPopup)
            {
                active.DidPopup = true;
                Dirty(uid, active);

                if (_net.IsServer)
                    _popup.PopupEntity(Loc.GetString("rmc-xeno-invisibility-expire-bump"), uid, uid, PopupType.MediumCaution);
            }

            RemoveInvisibility((uid, active), active.FullCooldown);
        }
    }
}
