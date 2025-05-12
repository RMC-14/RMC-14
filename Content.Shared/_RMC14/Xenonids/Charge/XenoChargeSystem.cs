using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Damage.ObstacleSlamming;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Xenonids.Animation;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Charge;

public sealed class XenoChargeSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _rmcEmote = default!;
    [Dependency] private readonly RMCObstacleSlammingSystem _rmcObstacleSlamming = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ThrownItemSystem _thrownItem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoAnimationsSystem _xenoAnimations = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;

    private readonly ProtoId<DamageTypePrototype> _blunt = "Blunt";

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<ThrownItemComponent> _thrownItemQuery;
    private EntityQuery<XenoToggleChargingComponent> _xenoToggleChargingQuery;
    private EntityQuery<XenoToggleChargingDamageComponent> _xenoToggleChargingDamageQuery;

    public override void Initialize()
    {
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _thrownItemQuery = GetEntityQuery<ThrownItemComponent>();
        _xenoToggleChargingQuery = GetEntityQuery<XenoToggleChargingComponent>();
        _xenoToggleChargingDamageQuery = GetEntityQuery<XenoToggleChargingDamageComponent>();

        SubscribeLocalEvent<XenoChargeComponent, XenoChargeActionEvent>(OnXenoChargeAction);
        SubscribeLocalEvent<XenoChargeComponent, ThrowDoHitEvent>(OnXenoChargeHit);
        SubscribeLocalEvent<XenoChargeComponent, XenoChargeDoAfterEvent>(OnXenoChargeDoAfterEvent);
        SubscribeLocalEvent<XenoChargeComponent, StopThrowEvent>(OnXenoChargeStop);

        SubscribeLocalEvent<XenoToggleChargingComponent, XenoToggleChargingActionEvent>(OnXenoToggleChargingAction);

        SubscribeLocalEvent<ActiveXenoToggleChargingComponent, MapInitEvent>(OnActiveToggleChargingMapInit);
        SubscribeLocalEvent<ActiveXenoToggleChargingComponent, ComponentRemove>(OnActiveToggleChargingRemove);
        SubscribeLocalEvent<ActiveXenoToggleChargingComponent, RefreshMovementSpeedModifiersEvent>(OnActiveToggleChargingSpeed);
        SubscribeLocalEvent<ActiveXenoToggleChargingComponent, MoveInputEvent>(OnActiveToggleChargingMoveInput);
        SubscribeLocalEvent<ActiveXenoToggleChargingComponent, MoveEvent>(OnActiveToggleChargingMove);
        SubscribeLocalEvent<ActiveXenoToggleChargingComponent, StartCollideEvent>(OnActiveToggleChargingCollide);
        SubscribeLocalEvent<ActiveXenoToggleChargingComponent, MobStateChangedEvent>(OnActiveToggleChargingMobStateChanged);
    }

    private void OnXenoChargeAction(Entity<XenoChargeComponent> xeno, ref XenoChargeActionEvent args)
    {
        if (args.Handled)
            return;

        var attempt = new XenoChargeAttemptEvent();
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        args.Handled = true;

        var ev = new XenoChargeDoAfterEvent(GetNetCoordinates(args.Target));
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.ChargeDelay, ev, xeno)
        {
            BreakOnMove = true,
            Hidden = true,
        };

        _stun.TrySlowdown(xeno, TimeSpan.FromSeconds(1.75f), false, 0f, 0f);
        _doAfter.TryStartDoAfter(doAfter);
    }


    private void OnXenoChargeHit(Entity<XenoChargeComponent> xeno, ref ThrowDoHitEvent args)
    {
        // TODO RMC14 lag compensation
        var targetId = args.Target;
        if (_mobState.IsDead(targetId))
            return;

        if (_physicsQuery.TryGetComponent(xeno, out var physics) &&
            _thrownItemQuery.TryGetComponent(xeno, out var thrown))
        {
            _thrownItem.LandComponent(xeno, thrown, physics, true);
            _thrownItem.StopThrow(xeno, thrown);
        }

        if (_timing.IsFirstTimePredicted && xeno.Comp.Charge is { } charge)
        {
            xeno.Comp.Charge = null;
            _xenoAnimations.PlayLungeAnimationEvent(xeno, charge);
        }

        if (!_xeno.CanAbilityAttackTarget(xeno, targetId, true))
            return;

        if (_net.IsServer)
            _audio.PlayPvs(xeno.Comp.Sound, xeno);

        var damage = _damageable.TryChangeDamage(targetId, xeno.Comp.Damage, origin: xeno, tool: xeno);
        if (damage?.GetTotal() > FixedPoint2.Zero)
        {
            var filter = Filter.Pvs(targetId, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
            _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { targetId }, filter);
        }

        _rmcPulling.TryStopAllPullsFromAndOn(targetId);

        var origin = _transform.GetMapCoordinates(xeno);
        var target = _transform.GetMapCoordinates(targetId);
        var diff = target.Position - origin.Position;
        diff = diff.Normalized() * xeno.Comp.Range;

        _stun.TryParalyze(targetId, xeno.Comp.StunTime, true);
        _throwing.TryThrow(targetId, diff, 10);
    }

    private void OnXenoChargeDoAfterEvent(Entity<XenoChargeComponent> xeno, ref XenoChargeDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        _rmcPulling.TryStopAllPullsFromAndOn(xeno);

        var coordinates = GetCoordinates(args.Coordinates);
        var origin = _transform.GetMapCoordinates(xeno);
        var diff = _transform.ToMapCoordinates(coordinates).Position - origin.Position;
        diff = diff.Normalized() * xeno.Comp.Range;

        xeno.Comp.Charge = diff;
        Dirty(xeno);

        _rmcObstacleSlamming.MakeImmune(xeno);
        _throwing.TryThrow(xeno, diff, xeno.Comp.Strength, animated: false);
    }

    private void OnXenoChargeStop(Entity<XenoChargeComponent> xeno, ref StopThrowEvent args)
    {
        if (xeno.Comp.Charge == null)
            return;

        foreach (var slower in _lookup.GetEntitiesInRange<MobStateComponent>(_transform.GetMapCoordinates(xeno), xeno.Comp.SlowRange))
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, slower))
                continue;

            _slow.TrySlowdown(slower, xeno.Comp.SlowTime, ignoreDurationModifier: true);
        }
    }

    private void OnXenoToggleChargingAction(Entity<XenoToggleChargingComponent> ent, ref XenoToggleChargingActionEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (RemComp<ActiveXenoToggleChargingComponent>(ent))
            return;

        if (!TryComp(ent, out InputMoverComponent? mover))
            return;

        var direction = mover.HeldMoveButtons & MoveButtons.AnyDirection;

        // Not moving
        if (direction == MoveButtons.None)
            return;

        var active = new ActiveXenoToggleChargingComponent();
        AddComp(ent, active, true);

        // Moving diagonally
        if ((direction & (direction - 1)) != MoveButtons.None)
            return;

        active.Direction = direction;
        Dirty(ent, active);
    }

    private void OnActiveToggleChargingMapInit(Entity<ActiveXenoToggleChargingComponent> ent, ref MapInitEvent args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(ent);

        foreach (var action in _actions.GetActions(ent))
        {
            if (action.Comp.BaseEvent is XenoToggleChargingActionEvent)
                _actions.SetToggled(action.Id, true);
        }
    }

    private void OnActiveToggleChargingRemove(Entity<ActiveXenoToggleChargingComponent> ent, ref ComponentRemove args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(ent);

        foreach (var action in _actions.GetActions(ent))
        {
            if (action.Comp.BaseEvent is XenoToggleChargingActionEvent)
                _actions.SetToggled(action.Id, false);
        }
    }

    private void OnActiveToggleChargingSpeed(Entity<ActiveXenoToggleChargingComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Stage == 0)
            return;

        if (!_xenoToggleChargingQuery.TryComp(ent, out var charging))
            return;

        var speed = 1 + ent.Comp.Stage * charging.SpeedPerStage;
        args.ModifySpeed(speed, speed);
    }

    private void OnActiveToggleChargingMoveInput(Entity<ActiveXenoToggleChargingComponent> ent, ref MoveInputEvent args)
    {
        var direction = args.Entity.Comp.HeldMoveButtons & MoveButtons.AnyDirection;

        // Same direction and not diagonal
        if (direction != MoveButtons.None &&
            (ent.Comp.Direction & direction) == direction &&
            (direction & (direction - 1)) == MoveButtons.None)
        {
            return;
        }

        var isPerpendicular = ent.Comp.Direction switch
        {
            MoveButtons.Up or MoveButtons.Down => direction is MoveButtons.Left or MoveButtons.Right,
            MoveButtons.Left or MoveButtons.Right => direction is MoveButtons.Up or MoveButtons.Down,
            _ => false,
        };

        if (isPerpendicular &&
            (ent.Comp.Deviated == MoveButtons.None || ent.Comp.Deviated == direction))
        {
            ent.Comp.Deviated = direction;
            return;
        }

        ent.Comp.Direction = direction;
        ResetCharging(ent);
    }

    private void OnActiveToggleChargingMove(Entity<ActiveXenoToggleChargingComponent> ent, ref MoveEvent args)
    {
        if (!_xenoToggleChargingQuery.TryComp(ent, out var charging))
            return;

        if (!args.OldPosition.TryDistance(EntityManager, _transform, args.NewPosition, out var distance))
            return;

        var absDistance = Math.Abs(distance);
        ent.Comp.Distance += absDistance;
        ent.Comp.LastMovedAt = _timing.CurTime;
        Dirty(ent);

        if (ent.Comp.Deviated != MoveButtons.None)
        {
            ent.Comp.DeviatedDistance += absDistance;
            if (ent.Comp.DeviatedDistance >= charging.MaxDeviation)
            {
                ResetCharging(ent);
                return;
            }
        }

        if (ent.Comp.Distance < charging.StepIncrement)
            return;

        ent.Comp.Steps += charging.StepIncrement;
        ent.Comp.Distance -= charging.StepIncrement;

        if (ent.Comp.Steps < charging.MinimumSteps)
            return;

        if (!_xenoPlasma.TryRemovePlasma(ent.Owner, charging.PlasmaPerStep))
        {
            ResetCharging(ent, false);
            return;
        }

        _rmcPulling.TryStopAllPullsFromAndOn(ent);
        if (ent.Comp.Stage == charging.MaxStage - 1 &&
            charging.Emote is { } emote)
        {
            _rmcEmote.TryEmoteWithChat(ent, emote, cooldown: charging.EmoteCooldown);
        }

        ent.Comp.Stage = Math.Min(charging.MaxStage, ent.Comp.Stage + 1);
        ent.Comp.SoundSteps += charging.StepIncrement;

        if (ent.Comp.Stage == 1 || ent.Comp.SoundSteps >= charging.SoundEvery)
        {
            ent.Comp.SoundSteps = 0;
            if (_timing.InSimulation)
                _audio.PlayPredicted(charging.Sound, ent, ent);
        }

        Dirty(ent);
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnActiveToggleChargingCollide(Entity<ActiveXenoToggleChargingComponent> ent, ref StartCollideEvent args)
    {
        if (Math.Abs(ent.Comp.Steps - 1) < 0.001)
            return;

        if (!_xenoToggleChargingDamageQuery.TryComp(args.OtherEntity, out var damage))
            return;

        if (ent.Comp.Stage < damage.MinimumStage)
            return;

        _audio.PlayPredicted(damage.Sound, _transform.GetMoverCoordinates(args.OtherEntity), ent);

        var damageable = CompOrNull<DamageableComponent>(args.OtherEntity);

        // TODO RMC14 this needs to keep the charge going if the entity is deleted (or queue deleted)
        if (damage.Destroy)
        {
            _popup.PopupClient(
                Loc.GetString("rmc-xeno-charge-plow-through", ("xeno", ent), ("target", args.OtherEntity)),
                args.OtherEntity,
                ent,
                PopupType.SmallCaution
            );

            if (_net.IsClient)
                _transform.DetachEntity(args.OtherEntity, Transform(args.OtherEntity));
            else
                QueueDel(args.OtherEntity);
        }
        else
        {
            _popup.PopupClient(
                Loc.GetString("rmc-xeno-charge-smashes", ("xeno", ent), ("target", args.OtherEntity)),
                args.OtherEntity,
                ent,
                PopupType.SmallCaution
            );

            if (damage.Damage != null)
                _damageable.TryChangeDamage(args.OtherEntity, damage.Damage * ent.Comp.Stage, damageable: damageable);

            if (damage.PercentageDamage > FixedPoint2.Zero &&
                _rmcDamageable.TryGetDestroyedAt(ent, out var destroyed))
            {
                var bluntDamage = new DamageSpecifier();
                bluntDamage.DamageDict[_blunt] = destroyed.Value * damage.PercentageDamage * ent.Comp.Stage;
                _damageable.TryChangeDamage(ent, bluntDamage, damageable: damageable);
            }
        }

        if (_net.IsClient &&
            damageable != null &&
            damage.DestroyDamage > FixedPoint2.Zero &&
            damageable.TotalDamage >= damage.DestroyDamage)
        {
            _transform.DetachEntity(args.OtherEntity, Transform(args.OtherEntity));
        }

        if (damage.Unanchor &&
            !TerminatingOrDeleted(args.OtherEntity) &&
            !EntityManager.IsQueuedForDeletion(args.OtherEntity))
        {
            _transform.Unanchor(args.OtherEntity);
        }

        if (damage.Stop)
        {
            ResetCharging(ent, false);
        }
        else if (damage.StageLoss > 0)
        {
            ent.Comp.Stage = Math.Max(0, ent.Comp.Stage - damage.StageLoss);
            Dirty(ent);
        }
    }

    private void OnActiveToggleChargingMobStateChanged(Entity<ActiveXenoToggleChargingComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        ResetCharging(ent);

        if (_timing.ApplyingState)
            return;

        RemComp<ActiveXenoToggleChargingComponent>(ent);
    }

    private void ResetCharging(Entity<ActiveXenoToggleChargingComponent> xeno, bool resetInput = true)
    {
        xeno.Comp.Steps = 0;
        xeno.Comp.SoundSteps = 0;
        xeno.Comp.Stage = 0;
        xeno.Comp.DeviatedDistance = 0;

        if (resetInput)
            xeno.Comp.Direction = MoveButtons.None;

        Dirty(xeno);
        _movementSpeed.RefreshMovementSpeedModifiers(xeno);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveXenoToggleChargingComponent, XenoToggleChargingComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var active, out var charging, out var physics))
        {
            if (physics.BodyStatus == BodyStatus.InAir)
                ResetCharging((uid, active));
            else if (time >= active.LastMovedAt + charging.LastMovedGrace)
                ResetCharging((uid, active), false);
        }
    }
}
