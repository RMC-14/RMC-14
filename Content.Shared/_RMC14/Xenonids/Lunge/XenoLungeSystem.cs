using Content.Shared._RMC14.Marines;
﻿using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Coordinates;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Lunge;

public sealed class XenoLungeSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ThrownItemSystem _thrownItem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<ThrownItemComponent> _thrownItemQuery;

    public override void Initialize()
    {
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _thrownItemQuery = GetEntityQuery<ThrownItemComponent>();

        SubscribeLocalEvent<XenoLungeComponent, XenoLungeActionEvent>(OnXenoLungeAction);
        SubscribeLocalEvent<XenoLungeComponent, ThrowDoHitEvent>(OnXenoLungeHit);

        SubscribeLocalEvent<XenoLungeStunnedComponent, PullStoppedMessage>(OnXenoLungeStunnedPullStopped);
    }

    private void OnXenoLungeAction(Entity<XenoLungeComponent> xeno, ref XenoLungeActionEvent args)
    {
        if (!_xeno.CanAbilityAttackTarget(xeno, args.Target))
            return;

        if (args.Handled)
            return;

        var attempt = new XenoLungeAttemptEvent();
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        args.Handled = true;

        var origin = _transform.GetMapCoordinates(xeno);
        var target = _transform.GetMapCoordinates(args.Target);
        var diff = target.Position - origin.Position;
        var length = diff.Length();
        diff *= xeno.Comp.Range / length;

        xeno.Comp.Charge = diff;
        Dirty(xeno);

        _throwing.TryThrow(xeno, diff, 10, animated: false);

        if (!_physicsQuery.TryGetComponent(xeno, out var physics))
            return;

        //Handle close-range or same-tile lunges
        foreach (var ent in _physics.GetContactingEntities(xeno.Owner, physics))
        {
            if (ent != args.Target)
                continue;

            if (ApplyLungeHitEffects(xeno, ent))
                return;
        }
    }

    private void OnXenoLungeHit(Entity<XenoLungeComponent> xeno, ref ThrowDoHitEvent args)
    {
        ApplyLungeHitEffects(xeno, args.Target);
    }

    private bool ApplyLungeHitEffects(Entity<XenoLungeComponent> xeno, EntityUid targetId)
    {
        // TODO RMC14 lag compensation
        if (_physicsQuery.TryGetComponent(xeno, out var physics) &&
            _thrownItemQuery.TryGetComponent(xeno, out var thrown))
        {
            _thrownItem.LandComponent(xeno, thrown, physics, true);
            _thrownItem.StopThrow(xeno, thrown);
        }

        if (_timing.IsFirstTimePredicted && xeno.Comp.Charge != null)
            xeno.Comp.Charge = null;

        if (_hive.FromSameHive(xeno.Owner, targetId))
            return true;


        if(_net.IsServer)
        {
            _stun.TryParalyze(targetId, xeno.Comp.StunTime, true);

            var stunned = EnsureComp<XenoLungeStunnedComponent>(targetId);
            stunned.ExpireAt = _timing.CurTime + xeno.Comp.StunTime;
            Dirty(targetId, stunned);
        }

        _pulling.TryStartPull(xeno, targetId);

        if (_net.IsServer &&
            HasComp<MarineComponent>(targetId))
        {
            SpawnAttachedTo(xeno.Comp.Effect, targetId.ToCoordinates());
        }

        return true;
    }

    private void OnXenoLungeStunnedPullStopped(Entity<XenoLungeStunnedComponent> ent, ref PullStoppedMessage args)
    {
        if (args.PulledUid != ent.Owner)
            return;

        foreach (var effect in ent.Comp.Effects)
        {
            _statusEffects.TryRemoveStatusEffect(ent, effect);
        }
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<XenoLungeStunnedComponent>();
        while (query.MoveNext(out var uid, out var stunned))
        {
            if (time < stunned.ExpireAt)
                continue;

            RemCompDeferred<XenoLungeStunnedComponent>(uid);
        }
    }
}
