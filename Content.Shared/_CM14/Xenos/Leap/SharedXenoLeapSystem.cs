using Content.Shared._CM14.Marines;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos.Leap;

public sealed class SharedXenoLeapSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly ThrownItemSystem _thrownItem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    private EntityQuery<MarineComponent> _marineQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<ThrownItemComponent> _thrownItemQuery;

    public override void Initialize()
    {
        _marineQuery = GetEntityQuery<MarineComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _thrownItemQuery = GetEntityQuery<ThrownItemComponent>();

        SubscribeLocalEvent<XenoLeapComponent, XenoLeapActionEvent>(OnXenoLeapAction);
        SubscribeLocalEvent<XenoLeapComponent, XenoLeapDoAfterEvent>(OnXenoLeapDoAfter);
        SubscribeLocalEvent<XenoLeapComponent, ThrowDoHitEvent>(OnXenoLeapDoHit);
    }
    private void OnXenoLeapAction(Entity<XenoLeapComponent> xeno, ref XenoLeapActionEvent args)
    {
        args.Handled = true;

        var ev = new XenoLeapDoAfterEvent(GetNetCoordinates(args.Target));
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.Delay, ev, xeno)
        {
            BreakOnUserMove = true,
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

        args.Handled = true;

        var origin = _transform.GetMapCoordinates(xeno);
        var target = GetCoordinates(args.Coordinates).ToMap(EntityManager, _transform);
        var gomen = target.Position - origin.Position;
        var length = gomen.Length();

        if (length > xeno.Comp.Range)
        {
            gomen *= xeno.Comp.Range.Float() / length;
        }

        _throwing.TryThrow(xeno, gomen, 30, user: xeno, pushbackRatio: 0);
    }

    private void OnXenoLeapDoHit(Entity<XenoLeapComponent> xeno, ref ThrowDoHitEvent args)
    {
        var marineId = args.Target;

        if (HasComp<LeapIncapacitatedComponent>(marineId) ||
            EnsureComp<LeapIncapacitatedComponent>(marineId, out var victim))
        {
            return;
        }

        if (!_marineQuery.TryGetComponent(marineId, out var marine))
            return;

        if (_physicsQuery.TryGetComponent(xeno, out var physics) &&
            _thrownItemQuery.TryGetComponent(xeno, out var thrown))
        {
            _thrownItem.LandComponent(xeno, thrown, physics, true);
            _thrownItem.StopThrow(xeno, thrown);
        }

        victim.RecoverAt = _timing.CurTime + xeno.Comp.KnockdownTime;

        _stun.TryKnockdown(marineId, xeno.Comp.KnockdownTime, true);
        _stun.TryStun(marineId, xeno.Comp.KnockdownTime, true);

        _audio.PlayPredicted(xeno.Comp.HitSound, xeno, xeno);

        var ev = new XenoLeapHitEvent((marineId, marine));
        RaiseLocalEvent(xeno, ref ev);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<LeapIncapacitatedComponent>();
        while (query.MoveNext(out var uid, out var victim))
        {
            if (victim.RecoverAt > time)
                continue;

            RemCompDeferred<LeapIncapacitatedComponent>(uid);
            _blindable.UpdateIsBlind(uid);
            _actionBlocker.UpdateCanMove(uid);
            _standing.Stand(uid);
        }
    }
}
