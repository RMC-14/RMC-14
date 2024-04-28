using System.Numerics;
using Content.Shared._CM14.Marines;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos.Leap;

public sealed class SharedXenoLeapSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<MarineComponent> _marineQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        _marineQuery = GetEntityQuery<MarineComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeLocalEvent<XenoLeapComponent, XenoLeapActionEvent>(OnXenoLeapAction);
        SubscribeLocalEvent<XenoLeapComponent, XenoLeapDoAfterEvent>(OnXenoLeapDoAfter);

        SubscribeLocalEvent<XenoLeapingComponent, StartCollideEvent>(OnXenoLeapingDoHit);
        SubscribeLocalEvent<XenoLeapingComponent, ComponentRemove>(OnXenoLeapingRemove);
        SubscribeLocalEvent<XenoLeapingComponent, PhysicsSleepEvent>(OnXenoLeapingPhysicsSleep);
        SubscribeLocalEvent<XenoLeapingComponent, StartPullAttemptEvent>(OnXenoLeapingStartPullAttempt);
        SubscribeLocalEvent<XenoLeapingComponent, PullAttemptEvent>(OnXenoLeapingPullAttempt);
    }

    private void OnXenoLeapAction(Entity<XenoLeapComponent> xeno, ref XenoLeapActionEvent args)
    {
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

        if (TryComp(xeno, out PullerComponent? puller) && TryComp(puller.Pulling, out PullableComponent? pullable))
            _pulling.TryStopPull(puller.Pulling.Value, pullable, xeno);

        var origin = _transform.GetMapCoordinates(xeno);
        var target = GetCoordinates(args.Coordinates).ToMap(EntityManager, _transform);
        var direction = target.Position - origin.Position;

        if (direction == Vector2.Zero)
            return;

        var length = direction.Length();
        var distance = Math.Clamp(length, 0.1f, xeno.Comp.Range.Float());
        direction *= distance / length;
        var impulse = direction.Normalized() * xeno.Comp.Strength * physics.Mass;

        leaping.Origin = _transform.GetMoverCoordinates(xeno);
        leaping.ParalyzeTime = xeno.Comp.KnockdownTime;
        leaping.HitSound = xeno.Comp.HitSound;
        leaping.LeapEndTime = _timing.CurTime + TimeSpan.FromSeconds(direction.Length() / xeno.Comp.Strength);

        _physics.ApplyLinearImpulse(xeno, impulse, body: physics);
        _physics.SetBodyStatus(xeno, physics, BodyStatus.InAir);
    }

    private void OnXenoLeapingDoHit(Entity<XenoLeapingComponent> xeno, ref StartCollideEvent args)
    {
        var marineId = args.OtherEntity;

        if (!_marineQuery.TryGetComponent(marineId, out var marine))
            return;

        if (HasComp<LeapIncapacitatedComponent>(marineId))
            return;

        if (xeno.Comp.KnockedDown)
            return;

        xeno.Comp.KnockedDown = true;
        Dirty(xeno);

        if (EnsureComp<LeapIncapacitatedComponent>(marineId, out var victim))
            return;

        if (_physicsQuery.TryGetComponent(xeno, out var physics))
        {
            _physics.SetBodyStatus(xeno, physics, BodyStatus.OnGround);

            if (physics.Awake)
                _broadphase.RegenerateContacts(xeno, physics);
        }

        victim.RecoverAt = _timing.CurTime + xeno.Comp.ParalyzeTime;

        _stun.TryParalyze(marineId, xeno.Comp.ParalyzeTime, true);
        _audio.PlayPredicted(xeno.Comp.HitSound, xeno, xeno);

        var ev = new XenoLeapHitEvent(xeno, (marineId, marine));
        RaiseLocalEvent(xeno, ref ev);
    }

    private void OnXenoLeapingRemove(Entity<XenoLeapingComponent> ent, ref ComponentRemove args)
    {
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

    private void StopLeap(Entity<XenoLeapingComponent> leaping)
    {
        if (_physicsQuery.TryGetComponent(leaping, out var physics))
        {
            _physics.SetLinearVelocity(leaping, Vector2.Zero, body: physics);
            _physics.SetBodyStatus(leaping, physics, BodyStatus.OnGround);
        }
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
            RemCompDeferred<XenoLeapingComponent>(uid);
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
