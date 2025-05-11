using Content.Shared._RMC14.Damage.ObstacleSlamming;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Xenonids.Animation;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Charge;

public sealed class XenoChargeSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly INetManager _net = default!;
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

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<ThrownItemComponent> _thrownItemQuery;
    private EntityQuery<XenoToggleChargingComponent> _xenoToggleChargingQuery;

    public override void Initialize()
    {
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _thrownItemQuery = GetEntityQuery<ThrownItemComponent>();
        _xenoToggleChargingQuery = GetEntityQuery<XenoToggleChargingComponent>();

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
    }

    private void OnActiveToggleChargingRemove(Entity<ActiveXenoToggleChargingComponent> ent, ref ComponentRemove args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnActiveToggleChargingSpeed(Entity<ActiveXenoToggleChargingComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var speed = ent.Comp.SpeedMultiplier;
        args.ModifySpeed(speed, speed);
    }

    private void OnActiveToggleChargingMoveInput(Entity<ActiveXenoToggleChargingComponent> ent, ref MoveInputEvent args)
    {
        var direction = args.Entity.Comp.HeldMoveButtons & MoveButtons.AnyDirection;

        // Same direction and not diagonal
        if ((ent.Comp.Direction & direction) == direction &&
            (direction & (direction - 1)) == MoveButtons.None)
        {
            return;
        }

        ent.Comp.Direction = direction;
        ent.Comp.SpeedMultiplier = 1;
        Dirty(ent);
    }

    private void OnActiveToggleChargingMove(Entity<ActiveXenoToggleChargingComponent> ent, ref MoveEvent args)
    {
        if (!_xenoToggleChargingQuery.TryComp(ent, out var charging))
            return;

        if (!args.OldPosition.TryDistance(EntityManager, _transform, args.NewPosition, out var distance))
            return;

        ent.Comp.Distance += Math.Abs(distance);
        Dirty(ent);

        if (ent.Comp.Distance < 1)
            return;

        ent.Comp.SpeedMultiplier += charging.SpeedPerStep;
        ent.Comp.Distance -= 1;
        Dirty(ent);
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
        Log.Info(ent.Comp.SpeedMultiplier.ToString());
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveXenoToggleChargingComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var active, out var physics))
        {
            if (physics.BodyStatus != BodyStatus.InAir)
                continue;

            active.SpeedMultiplier = 1;
            Dirty(uid, active);
        }
    }
}
