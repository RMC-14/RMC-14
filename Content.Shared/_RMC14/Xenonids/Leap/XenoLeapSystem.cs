using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Barricade;
using Content.Shared._RMC14.Barricade.Components;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.Damage.ObstacleSlamming;
using Content.Shared._RMC14.Entrenching;
using Content.Shared._RMC14.Movement;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Invisibility;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.ActionBlocker;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory.Events;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Leap;

public sealed class XenoLeapSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCLagCompensationSystem _rmcLagCompensation = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly DamageableSystem _damagable = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly RMCCameraShakeSystem _cameraShake = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;
    [Dependency] private readonly RMCObstacleSlammingSystem _obstacleSlamming = default!;
    [Dependency] private readonly SharedDirectionalAttackBlockSystem _directionalBlock = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<FixturesComponent> _fixturesQuery;

    public override void Initialize()
    {
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _fixturesQuery = GetEntityQuery<FixturesComponent>();

        SubscribeAllEvent<XenoLeapPredictedHitEvent>(OnPredictedHit);

        SubscribeLocalEvent<XenoLeapComponent, XenoLeapActionEvent>(OnXenoLeapAction);
        SubscribeLocalEvent<XenoLeapComponent, XenoLeapDoAfterEvent>(OnXenoLeapDoAfter);
        SubscribeLocalEvent<XenoLeapComponent, MeleeHitEvent>(OnXenoLeapMelee);

        SubscribeLocalEvent<RMCGrantLeapProtectionComponent, GotEquippedHandEvent>(OnEquippedHand);
        SubscribeLocalEvent<RMCGrantLeapProtectionComponent, GotUnequippedHandEvent>(OnUnequippedHand);
        SubscribeLocalEvent<RMCGrantLeapProtectionComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<RMCGrantLeapProtectionComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<RMCLeapProtectionComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCLeapProtectionComponent, XenoLeapHitAttempt>(OnXenoLeapHitAttempt);

        SubscribeLocalEvent<XenoLeapingComponent, StartCollideEvent>(OnXenoLeapingDoHit);
        SubscribeLocalEvent<XenoLeapingComponent, ComponentRemove>(OnXenoLeapingRemove);
        SubscribeLocalEvent<XenoLeapingComponent, PhysicsSleepEvent>(OnXenoLeapingPhysicsSleep);
        SubscribeLocalEvent<XenoLeapingComponent, StartPullAttemptEvent>(OnXenoLeapingStartPullAttempt);
        SubscribeLocalEvent<XenoLeapingComponent, PullAttemptEvent>(OnXenoLeapingPullAttempt);
    }

    private void OnPredictedHit(XenoLeapPredictedHitEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } ent)
            return;

        if (!TryComp(ent, out XenoLeapingComponent? leaping))
            return;

        if (GetEntity(msg.Target) is not { Valid: true } target)
            return;

        if (_net.IsServer)
        {
            if (!HasComp<XenoLeapComponent>(ent) || !leaping.Running)
                return;

            _rmcLagCompensation.SetLastRealTick(args.SenderSession.UserId, msg.LastRealTick);
            if (!_rmcLagCompensation.Collides(target, ent, args.SenderSession))
                return;
        }

        ApplyLeapingHitEffects((ent, leaping), target);
    }

    private void OnXenoLeapAction(Entity<XenoLeapComponent> xeno, ref XenoLeapActionEvent args)
    {
        if (args.Handled)
            return;

        var attempt = new XenoLeapAttemptEvent();
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        if (xeno.Comp.PlasmaCost > FixedPoint2.Zero &&
            !_xenoPlasma.HasPlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
        {
            return;
        }

        args.Handled = true;

        var ev = new XenoLeapDoAfterEvent(GetNetCoordinates(args.Target));
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.Delay, ev, xeno)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            DamageThreshold = FixedPoint2.New(10)
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnXenoLeapDoAfter(Entity<XenoLeapComponent> xeno, ref XenoLeapDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-leap-cancelled"), xeno, xeno);
            return;
        }

        if (!_physicsQuery.TryGetComponent(xeno, out var physics))
            return;

        if (EnsureComp<XenoLeapingComponent>(xeno, out var leaping))
            return;

        args.Handled = true;

        leaping.KnockdownRequiresInvisibility = xeno.Comp.KnockdownRequiresInvisibility;
        leaping.DestroyObjects = xeno.Comp.DestroyObjects;
        leaping.MoveDelayTime = xeno.Comp.MoveDelayTime;
        leaping.Damage = xeno.Comp.Damage;
        leaping.HitEffect = xeno.Comp.HitEffect;
        leaping.TargetJitterTime = xeno.Comp.TargetJitterTime;
        leaping.TargetCameraShakeStrength = xeno.Comp.TargetCameraShakeStrength;
        leaping.IgnoredCollisionGroupLarge = xeno.Comp.IgnoredCollisionGroupLarge;
        leaping.IgnoredCollisionGroupSmall = xeno.Comp.IgnoredCollisionGroupSmall;

        if (xeno.Comp.PlasmaCost > FixedPoint2.Zero &&
            !_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
        {
            return;
        }

        _rmcPulling.TryStopAllPullsFromAndOn(xeno);

        var origin = _transform.GetMapCoordinates(xeno);
        var target = _transform.ToMapCoordinates(args.Coordinates);
        var direction = target.Position - origin.Position;

        if (direction == Vector2.Zero)
            return;

        var length = direction.Length();
        var distance = Math.Clamp(length, 0.1f, xeno.Comp.Range.Float());
        direction *= distance / length;
        var impulse = direction.Normalized() * xeno.Comp.Strength * physics.Mass;

        leaping.Origin = _transform.GetMoverCoordinates(xeno);
        leaping.ParalyzeTime = xeno.Comp.KnockdownTime;
        leaping.LeapSound = xeno.Comp.LeapSound;
        leaping.LeapEndTime = _timing.CurTime + TimeSpan.FromSeconds(direction.Length() / xeno.Comp.Strength);

        _obstacleSlamming.MakeImmune(xeno, 0.5f);
        _physics.ApplyLinearImpulse(xeno, impulse, body: physics);
        _physics.SetBodyStatus(xeno, physics, BodyStatus.InAir);

        if (TryComp(xeno, out FixturesComponent? fixtures))
        {
            var collisionGroup = (int) leaping.IgnoredCollisionGroupSmall;
            if (_size.TryGetSize(xeno, out var size) && size > RMCSizes.SmallXeno)
                collisionGroup = (int) leaping.IgnoredCollisionGroupLarge;

            var fixture = fixtures.Fixtures.First();
            _physics.SetCollisionMask(xeno, fixture.Key, fixture.Value, fixture.Value.CollisionMask ^ collisionGroup);
        }

        //Handle close-range or same-tile leaps
        foreach (var ent in _physics.GetContactingEntities(xeno.Owner, physics))
        {
            if (_hive.FromSameHive(xeno.Owner, ent))
                continue;

            if (ApplyLeapingHitEffects((xeno, leaping), ent))
                return;
        }
    }

    private void OnXenoLeapMelee(Entity<XenoLeapComponent> xeno, ref MeleeHitEvent args)
    {
        if (!xeno.Comp.UnrootOnMelee)
            return;

        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        foreach (var entity in args.HitEntities)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, entity))
                return;

            if (TryComp<SlowedDownComponent>(xeno, out var root) && root.SprintSpeedModifier == 0f)
            {
                RemComp<SlowedDownComponent>(xeno);
                _movementSpeed.RefreshMovementSpeedModifiers(xeno);
            }
            break;
        }
    }

    private void OnXenoLeapingDoHit(Entity<XenoLeapingComponent> xeno, ref StartCollideEvent args)
    {
        ApplyLeapingHitEffects(xeno, args.OtherEntity);
    }

    private void OnXenoLeapingRemove(Entity<XenoLeapingComponent> ent, ref ComponentRemove args)
    {
        var ev = new XenoLeapStoppedEvent();
        RaiseLocalEvent(ent, ref ev);

        StopLeap(ent);
    }

    private void OnXenoLeapingPhysicsSleep(Entity<XenoLeapingComponent> ent, ref PhysicsSleepEvent args)
    {
        StopLeap(ent);
    }

    private void OnXenoLeapingStartPullAttempt(Entity<XenoLeapingComponent> ent, ref StartPullAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnXenoLeapingPullAttempt(Entity<XenoLeapingComponent> ent, ref PullAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnXenoLeapHitAttempt(Entity<RMCLeapProtectionComponent> ent, ref XenoLeapHitAttempt args)
    {
        if(args.Cancelled)
            return;

        if(!TryComp(args.Leaper, out XenoLeapingComponent? leaping))
            return;

        args.Cancelled = AttemptBlockLeap(ent.Owner, ent.Comp.StunDuration, ent.Comp.BlockSound, args.Leaper, leaping.Origin, ent.Comp.FullProtection);
    }

    private void OnGotEquipped(Entity<RMCGrantLeapProtectionComponent> ent, ref GotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;

        ApplyLeapProtection(args.Equipee, ent);
    }

    private void OnGotUnequipped(Entity<RMCGrantLeapProtectionComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;

        if(!RemoveLeapProtection(args.Equipee, ent))
            return;

        RemCompDeferred<RMCLeapProtectionComponent>(args.Equipee);
    }

    private void OnEquippedHand(Entity<RMCGrantLeapProtectionComponent> ent, ref GotEquippedHandEvent args)
    {
        if(!ent.Comp.ProtectsInHand)
            return;

        ApplyLeapProtection(args.User, ent);
    }

    private void OnUnequippedHand(Entity<RMCGrantLeapProtectionComponent> ent, ref GotUnequippedHandEvent args)
    {
        if(!ent.Comp.ProtectsInHand)
            return;

        if(!RemoveLeapProtection(args.User, ent))
            return;

        RemCompDeferred<RMCLeapProtectionComponent>(args.User);
    }

    private void OnMapInit(Entity<RMCLeapProtectionComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.InherentStunDuration == null)
            return;

        ent.Comp.StunDuration = ent.Comp.InherentStunDuration.Value;
    }

    /// <summary>
    ///     Apply the <see cref="RMCLeapProtectionComponent"/> to the given entity.
    /// </summary>
    /// <param name="receiver">The entity that receives the <see cref="RMCLeapProtectionComponent"/></param>
    /// <param name="protection">The entity that provides the component using <see cref="RMCGrantLeapProtectionComponent"/></param>
    private void ApplyLeapProtection(EntityUid receiver, Entity<RMCGrantLeapProtectionComponent> protection)
    {
        var leapProtection = EnsureComp<RMCLeapProtectionComponent>(receiver);
        leapProtection.ProtectionProviders.Add(protection);

        if (protection.Comp.StunDuration >= leapProtection.StunDuration)
        {
            leapProtection.StunDuration = protection.Comp.StunDuration;
            leapProtection.BlockSound = protection.Comp.BlockSound;
        }

        Dirty(receiver, leapProtection);
    }

    /// <summary>
    ///     Remove the entity with the <see cref="RMCGrantLeapProtectionComponent"/> from the list of entities that
    ///     provide the <see cref="RMCLeapProtectionComponent"/> to the user.
    ///     Then check if there are any other entities equipped that provide the component.
    /// </summary>
    /// <param name="user">The entity that unequipped an entity with <see cref="RMCGrantLeapProtectionComponent"/></param>
    /// <param name="protection">The entity that got unequipped</param>
    /// <returns>If the entity should keep the <see cref="RMCLeapProtectionComponent"/></returns>
    private bool RemoveLeapProtection(EntityUid user, Entity<RMCGrantLeapProtectionComponent> protection)
    {
        if (!TryComp(user, out RMCLeapProtectionComponent? leapProtection))
            return true;

        var stunDuration = new TimeSpan();
        leapProtection.ProtectionProviders.Remove(protection);

        // Set the inherent stun duration if it exists.
        if (leapProtection.InherentStunDuration != null)
        {
            stunDuration = leapProtection.InherentStunDuration.Value;
            leapProtection.InherentBlockSound = leapProtection.InherentBlockSound;
        }

        // Check if there are any other entities equipped that provide leap protection.
        foreach (var protectionGranter in leapProtection.ProtectionProviders)
        {
            if (!TryComp(protectionGranter, out RMCGrantLeapProtectionComponent? grantProtection) ||
                grantProtection.StunDuration < stunDuration)
                continue;

            stunDuration = grantProtection.StunDuration;
            leapProtection.BlockSound = grantProtection.BlockSound;
        }

        // Don't remove the component if it's inherent or if another component provides protection.
        if (stunDuration != TimeSpan.Zero)
        {
            leapProtection.StunDuration = stunDuration;
            Dirty(user, leapProtection);
            return false;
        }

        return true;
    }

    public bool AttemptBlockLeap(EntityUid blocker, TimeSpan stunDuration, SoundSpecifier blockSound, EntityUid leaper, EntityCoordinates leapOrigin, bool omnidirectionalProtection = false)
    {
        if (!_directionalBlock.IsFacingTarget(blocker, leaper, leapOrigin) && !omnidirectionalProtection)
            return false;

        if (HasComp<BarricadeComponent>(blocker) && (!TryComp(blocker, out BarbedComponent? barbed) || !barbed.IsBarbed))
            return false;

        if (_size.TryGetSize(leaper, out var size) && size >= RMCSizes.Big && !HasComp<BarbedComponent>(blocker))
            return false;

        var blockerCoordinates = _transform.GetMapCoordinates(blocker, Transform(blocker));

        if (size < RMCSizes.Big)
            _stun.TryParalyze(leaper, stunDuration, true);

        _size.KnockBack(leaper, blockerCoordinates, ignoreSize: true);
        _audio.PlayPredicted(blockSound, leaper, leaper);

        var selfMessage = Loc.GetString("rmc-obstacle-slam-self", ("object", Identity.Name(blocker, EntityManager, leaper)));

        _popup.PopupClient(selfMessage, leaper, leaper, PopupType.MediumCaution);

        var others = Filter.PvsExcept(leaper).Recipients;
        foreach (var other in others)
        {
            if (other.AttachedEntity is not { } otherEnt)
                continue;

            var othersMessage = Loc.GetString("rmc-obstacle-slam-others", ("ent", Identity.Name(leaper, EntityManager, otherEnt)), ("object", Identity.Name(blocker, EntityManager, otherEnt)));
            _popup.PopupEntity(othersMessage, leaper, otherEnt, PopupType.MediumCaution);
        }

        return true;
    }

    private bool IsValidLeapHit(Entity<XenoLeapingComponent> xeno, EntityUid target)
    {
        if (xeno.Comp.KnockedDown)
            return false;

        if (xeno.Comp.DestroyObjects && TryComp<XenoLeapDestroyOnPassComponent>(target, out var destroy))
        {
            if (_net.IsServer)
            {
                for (var i = 0; i < destroy.Amount; i++)
                {
                    if (destroy.SpawnPrototype != null)
                        SpawnAtPosition(destroy.SpawnPrototype, target.ToCoordinates());
                }

                QueueDel(target);
            }

            _physics.SetCanCollide(target, false, force: true);
            return false;
        }

        if (_standing.IsDown(target))
            return false;

        if (HasComp<LeapIncapacitatedComponent>(target))
            return false;

        if (_size.TryGetSize(target, out var size) && size >= RMCSizes.Big)
            return false;

        if (HasComp<XenoWeedsComponent>(target) || HasComp<XenoConstructComponent>(target))
            return false;

        return true;
    }

    private bool ApplyLeapingHitEffects(Entity<XenoLeapingComponent> xeno, EntityUid target)
    {
        if (!IsValidLeapHit(xeno, target))
            return false;

        if (_hive.FromSameHive(xeno.Owner, target))
        {
            StopLeap(xeno);
            return true;
        }

        var leapEv = new XenoLeapHitAttempt(xeno.Owner);
        RaiseLocalEvent(target, ref leapEv);

        if (leapEv.Cancelled)
        {
            xeno.Comp.KnockedDown = true;
            StopLeap(xeno);
            Dirty(xeno);
            return true;
        }

        if (!HasComp<MobStateComponent>(target) || _mobState.IsIncapacitated(target))
            return false;

        xeno.Comp.KnockedDown = true;
        Dirty(xeno);

        if (_physicsQuery.TryGetComponent(xeno, out var physics))
        {
            _physics.SetBodyStatus(xeno, physics, BodyStatus.OnGround);

            if (physics.Awake)
                _broadphase.RegenerateContacts(xeno, physics);
        }

        if (!xeno.Comp.KnockdownRequiresInvisibility || HasComp<XenoActiveInvisibleComponent>(xeno))
        {
            var victim = EnsureComp<LeapIncapacitatedComponent>(target);
            victim.RecoverAt = _timing.CurTime + xeno.Comp.ParalyzeTime;
            Dirty(target, victim);

            _stun.TrySlowdown(xeno, xeno.Comp.MoveDelayTime, true, 0f, 0f);

            if (_net.IsServer)
                _stun.TryParalyze(target, _xeno.TryApplyXenoDebuffMultiplier(target, xeno.Comp.ParalyzeTime), true);
        }

        if (xeno.Comp.HitEffect != null)
        {
            if (_net.IsServer)
                SpawnAttachedTo(xeno.Comp.HitEffect, target.ToCoordinates());
        }

        var damage = _damagable.TryChangeDamage(target, _xeno.TryApplyXenoSlashDamageMultiplier(target, xeno.Comp.Damage), origin: xeno, tool: xeno);
        if (damage?.GetTotal() > FixedPoint2.Zero)
        {
            var filter = Filter.Pvs(target, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
            _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { target }, filter);
        }

        _jitter.DoJitter(target, xeno.Comp.TargetJitterTime, false);
        _cameraShake.ShakeCamera(target, 2, xeno.Comp.TargetCameraShakeStrength);

        var ev = new XenoLeapHitEvent(xeno, target);
        RaiseLocalEvent(xeno, ref ev);

        if (!xeno.Comp.PlayedSound && _net.IsServer)
        {
            xeno.Comp.PlayedSound = true;
            _audio.PlayPvs(xeno.Comp.LeapSound, xeno);
        }

        if (_net.IsClient)
        {
            var predictedEv = new XenoLeapPredictedHitEvent(GetNetEntity(target), _rmcLagCompensation.GetLastRealTick(null));
            RaiseNetworkEvent(predictedEv);
            if (_timing.InPrediction && _timing.IsFirstTimePredicted)
            {
                RaisePredictiveEvent(predictedEv);
            }
        }

        StopLeap(xeno);
        return true;
    }

    private void StopLeap(Entity<XenoLeapingComponent> leaping)
    {
        if (_physicsQuery.TryGetComponent(leaping, out var physics))
        {
            _physics.SetLinearVelocity(leaping, Vector2.Zero, body: physics);
            _physics.SetBodyStatus(leaping, physics, BodyStatus.OnGround);
        }

        if (_fixturesQuery.TryGetComponent(leaping, out var fixtures))
        {
            var collisionGroup = (int)leaping.Comp.IgnoredCollisionGroupSmall;
            if (_size.TryGetSize(leaping, out var size) && size > RMCSizes.SmallXeno)
                collisionGroup = (int)leaping.Comp.IgnoredCollisionGroupLarge;

            if (size >= RMCSizes.SmallXeno)
            {
                var fixture = fixtures.Fixtures.First();
                _physics.SetCollisionMask(leaping, fixture.Key, fixture.Value, fixture.Value.CollisionMask | collisionGroup);
            }
        }

        RemCompDeferred<XenoLeapingComponent>(leaping);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var leaping = EntityQueryEnumerator<XenoLeapingComponent>();
        while (leaping.MoveNext(out var uid, out var comp))
        {
            if (time < comp.LeapEndTime)
                continue;

            StopLeap((uid, comp));
        }

        if (_net.IsClient)
            return;

        var incapacitated = EntityQueryEnumerator<LeapIncapacitatedComponent>();
        while (incapacitated.MoveNext(out var uid, out var victim))
        {
            if (victim.RecoverAt > time)
                continue;

            RemCompDeferred<LeapIncapacitatedComponent>(uid);
            _blindable.UpdateIsBlind(uid);
            _actionBlocker.UpdateCanMove(uid);
        }
    }
}

[ByRefEvent]
public record struct XenoLeapHitAttempt(EntityUid Leaper, bool Cancelled = false);

[ByRefEvent]
public record struct XenoLeapStoppedEvent;
