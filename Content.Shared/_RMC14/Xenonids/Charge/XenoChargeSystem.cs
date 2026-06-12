using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Damage.ObstacleSlamming;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.Animation;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Charge;

public sealed class XenoChargeSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly RMCObstacleSlammingSystem _rmcObstacleSlamming = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
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
    [Dependency] private readonly SharedDestructibleSystem _destruct = default!;
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;

    private readonly ProtoId<DamageTypePrototype> _blunt = "Blunt";

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<ThrownItemComponent> _thrownItemQuery;

    public override void Initialize()
    {
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _thrownItemQuery = GetEntityQuery<ThrownItemComponent>();

        SubscribeLocalEvent<XenoChargeComponent, XenoChargeActionEvent>(OnXenoChargeAction);
        SubscribeLocalEvent<XenoChargeComponent, ThrowDoHitEvent>(OnXenoChargeHit);
        SubscribeLocalEvent<XenoChargeComponent, XenoChargeDoAfterEvent>(OnXenoChargeDoAfterEvent);
        SubscribeLocalEvent<XenoChargeComponent, StopThrowEvent>(OnXenoChargeStop);
        SubscribeLocalEvent<XenoChargeComponent, PreventCollideEvent>(OnXenoChargePreventCollide);
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

    private void OnXenoChargeDoAfterEvent(Entity<XenoChargeComponent> xeno, ref XenoChargeDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        _rmcPulling.TryStopAllPullsFromAndOn(xeno);

        var coordinates = GetCoordinates(args.Coordinates);
        var origin = _transform.GetMapCoordinates(xeno);
        var diff = _transform.ToMapCoordinates(coordinates).Position - origin.Position;
        var length = diff.Length();
        if (length > xeno.Comp.Range)
            diff = diff.Normalized() * xeno.Comp.Range;
        else
            diff = diff.Normalized() * MathF.Ceiling(length);

        if (_net.IsServer)
        {
            var direction = diff.Normalized();
            var results = _physics.IntersectRay(
                origin.MapId,
                new CollisionRay(origin.Position, direction, (int) CollisionGroup.BarricadeImpassable),
                diff.Length(),
                xeno.Owner,
                false
            );

            foreach (var result in results)
            {
                if (!TryComp<XenoCrusherChargableComponent>(result.HitEntity, out var chargable))
                    continue;
                if (chargable.InstantDestroy)
                    continue;

                // Apply damage manually since we're stopping before impact
                if (chargable.SetDamage != null)
                {
                    xeno.Comp.AlreadyHit.Add(result.HitEntity);
                    _damageable.TryChangeDamage(result.HitEntity, chargable.SetDamage, origin: xeno, tool: xeno);
                    _audio.PlayPvs(xeno.Comp.Sound, xeno);
                }
                else
                {
                    _damageable.TryChangeDamage(result.HitEntity, xeno.Comp.Damage, origin: xeno, tool: xeno);
                    _audio.PlayPvs(xeno.Comp.Sound, xeno);
                }

                diff = direction * Math.Max(0, result.Distance - 0.5f);
                break;
            }
        }
        xeno.Comp.Charge = diff;
        Dirty(xeno);

        EnsureComp<XenoChargingComponent>(xeno);

        _rmcObstacleSlamming.MakeImmune(xeno);
        _throwing.TryThrow(xeno, diff, xeno.Comp.Strength, animated: false, compensateFriction: true);
    }

    private void OnXenoChargeHit(Entity<XenoChargeComponent> xeno, ref ThrowDoHitEvent args)
    {
        // TODO RMC14 lag compensation
        // TODO RMC14 allow charge to continue if pass is true
        var targetId = args.Target;
        if (_mobState.IsDead(targetId))
            return;

        XenoCrusherChargableComponent? crush = null;
        var isValidTarget = _xeno.CanAbilityAttackTarget(xeno, targetId);

        if (!isValidTarget)
        {
            TryComp(targetId, out crush);
            if (crush == null && !HasComp<DamageableComponent>(targetId))
                return;
            if (HasComp<XenoComponent>(targetId))
                return;
        }

        StopCrusherCharge(xeno);

        if (_net.IsServer)
            _audio.PlayPvs(xeno.Comp.Sound, xeno);

        var structDamage = xeno.Comp.Damage;

        if (crush != null)
        {
            if (crush.SetDamage != null)
                structDamage = crush.SetDamage;
        }

        //var finalDamage = _xeno.TryApplyXenoSlashDamageMultiplier(targetId, structDamage);
        var damage = _damageable.TryChangeDamage(targetId, structDamage, origin: xeno, tool: xeno, shouldIgnoreClawLogic: true);

        if (damage?.GetTotal() > FixedPoint2.Zero && !TerminatingOrDeleted(targetId))
        {
            var filter = Filter.Pvs(targetId, entityManager: EntityManager)
                .RemoveWhereAttachedEntity(o => o == xeno.Owner);
            _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { targetId }, filter);
        }

        if (crush != null && crush.DestroyDamage != null)
        {
            if (TryComp<DamageableComponent>(targetId, out var damageable))
            {
                if (damage != null && crush.PassOnDestroy &&
                    crush.DestroyDamage > FixedPoint2.Zero && damageable.TotalDamage >= crush.DestroyDamage)
                {
                    if (_net.IsClient)
                        _transform.DetachEntity(targetId, Transform(targetId));
                }
            }
        }

        _rmcPulling.TryStopAllPullsFromAndOn(targetId);

        var origin = _transform.GetMapCoordinates(xeno);

        _stun.TryParalyze(targetId, xeno.Comp.StunTime, true);
        _sizeStun.KnockBack(targetId,
            origin,
            xeno.Comp.KnockBackDistance,
            xeno.Comp.KnockBackDistance,
            knockBackSpeed: 15);
    }

    private void OnXenoChargeStop(Entity<XenoChargeComponent> xeno, ref StopThrowEvent args)
    {
        if (xeno.Comp.Charge == null)
            return;

        foreach (var slower in _lookup.GetEntitiesInRange<MobStateComponent>(_transform.GetMapCoordinates(xeno),
                     xeno.Comp.SlowRange))
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, slower))
                continue;

            _slow.TrySlowdown(slower, xeno.Comp.SlowTime, ignoreDurationModifier: true);
        }

        xeno.Comp.Charge = null;
        RemComp<XenoChargingComponent>(xeno);
        Dirty(xeno);
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

    private void OnXenoChargePreventCollide(Entity<XenoChargeComponent> xeno, ref PreventCollideEvent args)
    {
        if (xeno.Comp.Charge == null)
            return;

        if (TerminatingOrDeleted(args.OtherEntity))
            return;

        // Pass through friendly/invalid mob targets
        if (_hive.FromSameHive(xeno.Owner, args.OtherEntity)
            && HasComp<XenoComponent>(args.OtherEntity))
        {
            args.Cancelled = true;
            return;
        }

        if (!TryComp(args.OtherEntity, out XenoCrusherChargableComponent? crush))
            return;

        if (!crush.InstantDestroy || !crush.PassOnDestroy)
            return;

        if (_net.IsServer)
            _destruct.DestroyEntity(args.OtherEntity);
        else if (_net.IsClient)
            _transform.DetachEntity(args.OtherEntity, Transform(args.OtherEntity));

        args.Cancelled = true;
    }
}
