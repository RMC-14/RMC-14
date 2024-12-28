using Content.Shared._RMC14.Pulling;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Punch;

public sealed class XenoPunchSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoPunchComponent, XenoPunchActionEvent>(OnXenoPunchAction);

        SubscribeLocalEvent<PunchSlowedComponent, ComponentRemove>(OnPunchSlowRemoved);
        SubscribeLocalEvent<PunchSlowedComponent, RefreshMovementSpeedModifiersEvent>(OnPunchSlowRefresh);
    }

    private void OnXenoPunchAction(Entity<XenoPunchComponent> xeno, ref XenoPunchActionEvent args)
    {
        if (!_xeno.CanAbilityAttackTarget(xeno, args.Target))
            return;

        if (args.Handled)
            return;

        var attempt = new XenoPunchAttemptEvent();
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        args.Handled = true;

        if (_net.IsServer)
            _audio.PlayPvs(xeno.Comp.Sound, xeno);

        var targetId = args.Target;
        _rmcPulling.TryStopAllPullsFromAndOn(targetId);

        var damage = _damageable.TryChangeDamage(targetId, xeno.Comp.Damage);
        if (damage?.GetTotal() > FixedPoint2.Zero)
        {
            var filter = Filter.Pvs(targetId, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
            _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { targetId }, filter);
        }

        var origin = _transform.GetMapCoordinates(xeno);
        var target = _transform.GetMapCoordinates(targetId);
        var diff = target.Position - origin.Position;
        diff = diff.Normalized() * xeno.Comp.Range;

        _throwing.TryThrow(targetId, diff, 10);

        var slow = EnsureComp<PunchSlowedComponent>(targetId);
        slow.ExpiresAt = _timing.CurTime + xeno.Comp.SlowDuration;
        _speed.RefreshMovementSpeedModifiers(targetId);

        if (_net.IsServer)
            SpawnAttachedTo(xeno.Comp.Effect, targetId.ToCoordinates());
    }

    private void OnPunchSlowRefresh(Entity<PunchSlowedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var modifier = ent.Comp.SlowAmount.Float();
        args.ModifySpeed(modifier, modifier);
    }

    private void OnPunchSlowRemoved(Entity<PunchSlowedComponent> ent, ref ComponentRemove args)
    {
        if (!TerminatingOrDeleted(ent))
            _speed.RefreshMovementSpeedModifiers(ent);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var slowed = EntityQueryEnumerator<PunchSlowedComponent>();

        while (slowed.MoveNext(out var uid, out var slow))
        {
            if (slow.ExpiresAt > time)
                continue;

            RemCompDeferred<PunchSlowedComponent>(uid);
        }
    }
}
