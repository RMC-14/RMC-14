using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Damage.ObstacleSlamming;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.Animation;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.HiveLeader;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Actions;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Destructible;
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
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Charge;

public sealed class XenoChargeSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedMoverController _moverController = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCActionsSystem _rmcActions = default!;
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
    [Dependency] private readonly SharedXenoHiveSystem _xenoHive = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly SharedDestructibleSystem _destruct = default!;
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;

    private readonly ProtoId<DamageTypePrototype> _blunt = "Blunt";

    private EntityQuery<InputMoverComponent> _inputMoverQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<ThrownItemComponent> _thrownItemQuery;
    private EntityQuery<XenoToggleChargingComponent> _xenoToggleChargingQuery;
    private EntityQuery<ActiveXenoToggleChargingComponent> _activeXenoToggleChargingQuery;
    private EntityQuery<XenoToggleChargingRecentlyHitComponent> _xenoToggleChargingRecentlyHitQuery;

    private bool _relativeMovement;
    private readonly HashSet<(Entity<ActiveXenoToggleChargingComponent> Crusher, EntityUid Target)> _hit = new();

    public override void Initialize()
    {
        _inputMoverQuery = GetEntityQuery<InputMoverComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _thrownItemQuery = GetEntityQuery<ThrownItemComponent>();
        _xenoToggleChargingQuery = GetEntityQuery<XenoToggleChargingComponent>();
        _activeXenoToggleChargingQuery = GetEntityQuery<ActiveXenoToggleChargingComponent>();
        _xenoToggleChargingRecentlyHitQuery = GetEntityQuery<XenoToggleChargingRecentlyHitComponent>();

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

        SubscribeLocalEvent<XenoToggleChargingDamageComponent, XenoToggleChargingCollideEvent>(OnChargingDamageCollide);

        SubscribeLocalEvent<XenoToggleChargingKnockbackComponent, XenoToggleChargingCollideEvent>(OnChargingKnockbackCollide);
        SubscribeLocalEvent<XenoToggleChargingKnockbackComponent, AttemptMobTargetCollideEvent>(OnChargingKnockbackAttemptCollide);

        SubscribeLocalEvent<XenoToggleChargingParalyzeComponent, XenoToggleChargingCollideEvent>(OnChargingParalyzeCollide);

        SubscribeLocalEvent<XenoToggleChargingStopComponent, XenoToggleChargingCollideEvent>(OnChargingStopCollide);

        SubscribeLocalEvent<HiveLeaderComponent, XenoToggleChargingCollideEvent>(OnLeaderCollide);

        Subs.CVar(_config, CCVars.RelativeMovement, v => _relativeMovement = v, true);
    }

    private void OnChargingDamageCollide(Entity<XenoToggleChargingDamageComponent> damage, ref XenoToggleChargingCollideEvent args)
    {
        args.Handled = true;

        var ent = args.Charger;
        if (ent.Comp.Stage < damage.Comp.MinimumStage)
            return;

        if (_net.IsServer)
            _audio.PlayPvs(damage.Comp.Sound, _transform.GetMoverCoordinates(damage));

        var damageable = CompOrNull<DamageableComponent>(damage);

        // TODO RMC14 this needs to keep the charge going if the entity is deleted (or queue deleted)
        if (damage.Comp.Destroy)
        {
            if (_net.IsServer)
            {
                _popup.PopupEntity(
                    Loc.GetString("rmc-xeno-charge-plow-through", ("xeno", ent), ("target", damage)),
                    damage,
                    PopupType.SmallCaution
                );
            }

            if (_net.IsClient)
                _transform.DetachEntity(damage, Transform(damage));
            else
                QueueDel(damage);
        }
        else
        {
            if (_net.IsServer)
            {
                _popup.PopupEntity(
                    Loc.GetString("rmc-xeno-charge-smashes", ("xeno", ent), ("target", damage)),
                    damage,
                    PopupType.SmallCaution
                );
            }

            var stage = ent.Comp.Stage;
            if (damage.Comp.StageMultipliers != null &&
                damage.Comp.StageMultipliers.TryGetValue(stage, out var stageMult))
            {
                stage = stageMult * stageMult;
            }
            else if (damage.Comp.DefaultMultiplier != 0)
            {
                stage = damage.Comp.DefaultMultiplier * damage.Comp.DefaultMultiplier;
            }
            else if (stage < damage.Comp.MinimumStage)
            {
                stage = damage.Comp.MinimumStage;
            }

            if (damage.Comp.Damage != null)
                _damageable.TryChangeDamage(damage, damage.Comp.Damage * stage, damageable: damageable);

            if (damage.Comp.ArmorPiercingDamage != null)
            {
                _damageable.TryChangeDamage(
                    damage,
                    damage.Comp.ArmorPiercingDamage * stage,
                    damageable: damageable,
                    armorPiercing: damage.Comp.ArmorPiercing
                );
            }

            if (damage.Comp.PercentageDamage > FixedPoint2.Zero &&
                _rmcDamageable.TryGetDestroyedAt(ent, out var destroyed))
            {
                var bluntDamage = new DamageSpecifier();
                bluntDamage.DamageDict[_blunt] = destroyed.Value * damage.Comp.PercentageDamage * stage;
                _damageable.TryChangeDamage(damage, bluntDamage, damageable: damageable);
            }
        }

        if (_net.IsClient &&
            damageable != null &&
            damage.Comp.DestroyDamage > FixedPoint2.Zero &&
            damageable.TotalDamage >= damage.Comp.DestroyDamage)
        {
            _transform.DetachEntity(damage, Transform(damage));
        }

        if (damage.Comp.Unanchor &&
            !TerminatingOrDeleted(damage) &&
            !EntityManager.IsQueuedForDeletion(damage))
        {
            _transform.Unanchor(damage);
        }

        if (damage.Comp.Stop)
            ResetCharging(ent, false);
        else if (damage.Comp.StageLoss > 0)
            IncrementStages(ent, -damage.Comp.StageLoss);
    }

    private void OnChargingKnockbackCollide(Entity<XenoToggleChargingKnockbackComponent> ent, ref XenoToggleChargingCollideEvent args)
    {
        args.Handled = true;

        if (TryComp(ent, out TransformComponent? xform) &&
            xform.Anchored)
        {
            ResetStage(args.Charger);
            return;
        }

        if (!ent.Comp.Enabled ||
            args.Charger.Comp.Stage == 0)
        {
            ResetStage(args.Charger);
            return;
        }

        // TODO RMC14 two chargers colliding
        _rmcPulling.TryStopAllPullsFromAndOn(ent);

        if (_xenoHive.FromSameHive(ent.Owner, args.Charger.Owner))
            _rmcObstacleSlamming.MakeImmune(ent);

        var direction = args.Charger.Comp.Direction;
        if (direction == DirectionFlag.None)
            return;

        // TODO RMC14 make this take into account relative position instead of being a 50/50 like 13
        var perpendiculars = direction.AsDir().GetPerpendiculars();
        var perpendicular = _random.Prob(0.5f) ? perpendiculars.First : perpendiculars.Second;
        var diff = perpendicular.ToVec().Normalized();

        _throwing.TryThrow(ent, diff, compensateFriction: true);
        IncrementStages(args.Charger, -1);

        if (_net.IsServer)
        {
            _audio.PlayPvs(ent.Comp.Sound, ent);
            // TODO RMC14 target msg
            // var userMsg = Loc.GetString("rmc-xeno-charge-knockback-self", ("target", ent));
            var othersMsg = Loc.GetString("rmc-xeno-charge-knockback-others", ("user", args.Charger), ("target", ent));
            _popup.PopupEntity(othersMsg, ent, PopupType.MediumCaution);
        }
    }

    private void OnChargingKnockbackAttemptCollide(Entity<XenoToggleChargingKnockbackComponent> ent, ref AttemptMobTargetCollideEvent args)
    {
        if (!_activeXenoToggleChargingQuery.TryComp(args.Entity, out var active) ||
            active.Stage <= 0)
        {
            return;
        }

        args.Cancelled = true;
    }

    private void OnChargingParalyzeCollide(Entity<XenoToggleChargingParalyzeComponent> ent, ref XenoToggleChargingCollideEvent args)
    {
        args.Handled = true;

        var stage = args.Charger.Comp.Stage;
        if (stage <= 0)
            return;

        if (!_xenoToggleChargingQuery.TryComp(args.Charger, out var charging))
            return;

        var duration = stage >= charging.MaxStage
            ? ent.Comp.MaxStageDuration
            : ent.Comp.Duration;
        _stun.TryParalyze(ent, duration, false);
    }

    private void OnChargingStopCollide(Entity<XenoToggleChargingStopComponent> ent, ref XenoToggleChargingCollideEvent args)
    {
        args.Handled = true;
        ResetStage(args.Charger);
    }

    private void OnLeaderCollide(Entity<HiveLeaderComponent> ent, ref XenoToggleChargingCollideEvent args)
    {
        args.Handled = true;
        ResetStage(args.Charger);
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

    private void StopCrusherCharge(Entity<XenoChargeComponent> xeno)
    {
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

    }

    private void OnXenoChargeHit(Entity<XenoChargeComponent> xeno, ref ThrowDoHitEvent args)
    {
        // TODO RMC14 lag compensation
        // TODO RMC14 allow charge to continue if pass is true
        var targetId = args.Target;
        if (_mobState.IsDead(targetId))
            return;

        StopCrusherCharge(xeno);

        XenoCrusherChargableComponent? crush = null;
        var pass = false;

        if (!_xeno.CanAbilityAttackTarget(xeno, targetId) && !TryComp(targetId, out crush))
        {
            return;
        }

        if (_net.IsServer)
            _audio.PlayPvs(xeno.Comp.Sound, xeno);

        var structDamage = xeno.Comp.Damage;

        if (crush != null)
        {
            if (crush.SetDamage != null)
                structDamage = crush.SetDamage;

            if(crush.InstantDestroy)
            {
                if (_net.IsClient && pass)
                    _transform.DetachEntity(targetId, Transform(targetId));
                else if (_net.IsServer)
                    _destruct.DestroyEntity(targetId);
                return;
            }

        }

        var damage = _damageable.TryChangeDamage(targetId, _xeno.TryApplyXenoSlashDamageMultiplier(targetId, structDamage), origin: xeno, tool: xeno);
        if (damage?.GetTotal() > FixedPoint2.Zero)
        {
            var filter = Filter.Pvs(targetId, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
            _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { targetId }, filter);
        }

        if (crush != null && crush.DestroyDamage != null)
        {
            if (TryComp<DamageableComponent>(targetId, out var damageable))
            {
                if (damage != null && crush.PassOnDestroy &&
                    crush.DestroyDamage > FixedPoint2.Zero && damageable.TotalDamage >= crush.DestroyDamage)
                {
                    pass = true;

                    if (_net.IsClient)
                        _transform.DetachEntity(targetId, Transform(targetId));
                }
            }
        }

        var range = xeno.Comp.Range;

        if (crush != null && crush.ThrowRange != null)
            range = crush.ThrowRange.Value;

        _rmcPulling.TryStopAllPullsFromAndOn(targetId);

        var origin = _transform.GetMapCoordinates(xeno);

        _stun.TryParalyze(targetId, xeno.Comp.StunTime, true);
        _sizeStun.KnockBack(targetId, origin, 2, 2, knockBackSpeed: 10);
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

        var direction = GetHeldButton(ent, mover.HeldMoveButtons);
        var active = new ActiveXenoToggleChargingComponent();
        AddComp(ent, active, true);

        // Moving diagonally
        if ((direction & (direction - 1)) != DirectionFlag.None)
            return;

        active.Direction = direction;
        Dirty(ent, active);
    }

    private void OnActiveToggleChargingMapInit(Entity<ActiveXenoToggleChargingComponent> ent, ref MapInitEvent args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(ent);

        foreach (var action in _rmcActions.GetActionsWithEvent<XenoToggleChargingActionEvent>(ent))
        {
            _actions.SetToggled((action, action), true);
        }
    }

    private void OnActiveToggleChargingRemove(Entity<ActiveXenoToggleChargingComponent> ent, ref ComponentRemove args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(ent);

        foreach (var action in _rmcActions.GetActionsWithEvent<XenoToggleChargingActionEvent>(ent))
        {
            _actions.SetToggled((action, action), false);
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
        var direction = GetHeldButton(ent, args.Entity.Comp.HeldMoveButtons & MoveButtons.AnyDirection);

        // Same direction and not diagonal
        if (direction != DirectionFlag.None &&
            (ent.Comp.Direction & direction) == direction &&
            (direction & (direction - 1)) == DirectionFlag.None)
        {
            return;
        }

        if (ent.Comp.Direction != DirectionFlag.None)
        {
            var perpendiculars = ent.Comp.Direction.AsDir().GetPerpendiculars();
            var isPerpendicular = ent.Comp.Direction == perpendiculars.First.AsFlag() ||
                                  ent.Comp.Direction == perpendiculars.Second.AsFlag();

            if (isPerpendicular &&
                (ent.Comp.Deviated == DirectionFlag.None || ent.Comp.Deviated == direction))
            {
                ent.Comp.Deviated = direction;
                return;
            }
        }

        ResetCharging(ent);
        ent.Comp.Direction = direction;
    }

    private void OnActiveToggleChargingMove(Entity<ActiveXenoToggleChargingComponent> ent, ref MoveEvent args)
    {
        if (!_xenoToggleChargingQuery.TryComp(ent, out var charging))
            return;

        if (_rmcPulling.IsBeingPulled(ent.Owner, out _))
            return;

        if (!args.OldPosition.TryDistance(EntityManager, _transform, args.NewPosition, out var distance))
            return;

        var absDistance = Math.Abs(distance);
        ent.Comp.Distance += absDistance;
        ent.Comp.LastMovedAt = _timing.CurTime;
        Dirty(ent);

        if (_inputMoverQuery.TryComp(ent, out var mover))
        {
            var lastRotation = ent.Comp.LastRelativeRotation;
            ent.Comp.LastRelativeRotation = mover.RelativeRotation;
            if (ent.Comp.LastRelativeRotation != lastRotation)
            {
                ResetStage(ent);
                return;
            }
        }

        if (ent.Comp.Deviated != DirectionFlag.None)
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

        _hit.Add((ent, args.OtherEntity));
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
        ResetStage(xeno);
        xeno.Comp.DeviatedDistance = 0;

        if (resetInput)
            xeno.Comp.Direction = DirectionFlag.None;

        Dirty(xeno);
        _movementSpeed.RefreshMovementSpeedModifiers(xeno);
    }

    private void ResetStage(Entity<ActiveXenoToggleChargingComponent> xeno)
    {
        xeno.Comp.Steps = 0;
        xeno.Comp.SoundSteps = 0;
        xeno.Comp.Stage = 0;

        Dirty(xeno);
        _movementSpeed.RefreshMovementSpeedModifiers(xeno);
    }

    private void IncrementStages(Entity<ActiveXenoToggleChargingComponent> ent, int increment)
    {
        ent.Comp.Stage = Math.Max(0, ent.Comp.Stage + increment);

        if (_xenoToggleChargingQuery.TryComp(ent, out var charging))
            ent.Comp.Stage = Math.Min(charging.MaxStage, ent.Comp.Stage);

        Dirty(ent);
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    private DirectionFlag GetHeldButton(EntityUid mover, MoveButtons button)
    {
        if (!TryComp(mover, out InputMoverComponent? moverComp))
            return DirectionFlag.None;

        var parentRotation = _moverController.GetParentGridAngle(moverComp);
        var total = _moverController.DirVecForButtons(button);
        var wishDir = _relativeMovement ? parentRotation.RotateVec(total) : total;
        return wishDir.GetDir().AsFlag();
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        try
        {
            foreach (var hit in _hit)
            {
                if (TerminatingOrDeleted(hit.Crusher) || TerminatingOrDeleted(hit.Target))
                    continue;

                if (_xenoToggleChargingRecentlyHitQuery.TryComp(hit.Target, out var recently) &&
                    time < recently.LastHitAt + recently.Cooldown)
                {
                    return;
                }

                var ev = new XenoToggleChargingCollideEvent(hit.Crusher);
                RaiseLocalEvent(hit.Target, ref ev);

                if (ev.Handled)
                {
                    recently = EnsureComp<XenoToggleChargingRecentlyHitComponent>(hit.Target);
                    recently.LastHitAt = time;
                    Dirty(hit.Target, recently);

                    if (hit.Crusher.Comp.Stage == 0)
                    {
                        hit.Crusher.Comp.Steps = 0;
                        Dirty(hit.Crusher);
                    }
                }
            }
        }
        finally
        {
            _hit.Clear();
        }

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
