using System.Numerics;
using Content.Shared._RMC14.Damage.ObstacleSlamming;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Lunge;

public sealed class XenoLungeSystem : EntitySystem
{
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
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
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly RMCObstacleSlammingSystem _rmcObstacleSlamming = default!;
    [Dependency] private readonly XenoLeapSystem _leap = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<ThrownItemComponent> _thrownItemQuery;

    public override void Initialize()
    {
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _thrownItemQuery = GetEntityQuery<ThrownItemComponent>();

        SubscribeLocalEvent<XenoLungeComponent, XenoLungeActionEvent>(OnXenoLungeAction);
        SubscribeLocalEvent<XenoLungeComponent, ThrowDoHitEvent>(OnXenoLungeHit);
        SubscribeLocalEvent<XenoLungeComponent, LandEvent>(OnXenoLungeLand);
        SubscribeLocalEvent<XenoLungeComponent, MeleeAttackAttemptEvent>(OnAttackAttempt);

        SubscribeLocalEvent<RMCLungeProtectionComponent, XenoLungeHitAttempt>(OnXenoLungeHitAttempt);

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

        _rmcPulling.TryStopAllPullsFromAndOn(xeno);

        var origin = _transform.GetMapCoordinates(xeno);
        var target = _transform.GetMapCoordinates(args.Target);
        var diff = target.Position - origin.Position;
        diff = diff.Normalized() * xeno.Comp.Range;

        xeno.Comp.Charge = diff;
        xeno.Comp.Target = args.Target;
        xeno.Comp.Origin = origin;
        Dirty(xeno);

        _rmcObstacleSlamming.MakeImmune(xeno, 0.5f);
        _throwing.TryThrow(xeno, diff, 30, animated: false);

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
        if (!_mobState.IsAlive(xeno) || HasComp<StunnedComponent>(xeno))
        {
            xeno.Comp.Charge = null;
            xeno.Comp.Target = null;
            return;
        }

        ApplyLungeHitEffects(xeno, args.Target);
    }

    private void OnXenoLungeLand(Entity<XenoLungeComponent> ent, ref LandEvent args)
    {
        if (ent.Comp.Charge == null && ent.Comp.Target == null)
            return;

        var target = ent.Comp.Target;
        ent.Comp.Charge = null;
        ent.Comp.Target = null;
        Dirty(ent);

        if (target == null || _pulling.IsPulling(ent))
            return;

        if (_interaction.InRangeUnobstructed(ent.Owner, target.Value))
            ApplyLungeHitEffects(ent, target.Value);
    }

    private bool ApplyLungeHitEffects(Entity<XenoLungeComponent> xeno, EntityUid targetId)
    {
        // TODO RMC14 lag compensation
        if (_mobState.IsDead(targetId))
            return false;

        if (xeno.Comp.Charge == null)
            return false;

        if (_physicsQuery.TryGetComponent(xeno, out var physics) &&
            _thrownItemQuery.TryGetComponent(xeno, out var thrown))
        {
            _thrownItem.LandComponent(xeno, thrown, physics, true);
            _thrownItem.StopThrow(xeno, thrown);
        }

        if (_timing.IsFirstTimePredicted && xeno.Comp.Charge != null)
            xeno.Comp.Charge = null;

        var ev = new XenoLungeHitAttempt(xeno);
        RaiseLocalEvent(targetId, ref ev);

        if (ev.Cancelled)
            return true;

        if (!_xeno.CanAbilityAttackTarget(xeno, targetId) || (_size.TryGetSize(targetId, out var size) && size >= RMCSizes.Big) ||
            (TryComp<XenoComponent>(targetId, out var xenoComp) && xenoComp.Tier >= 2)) //Fails if big or tier 2 or more
            return true;

        if (_net.IsServer)
        {
            var stunTime = _xeno.TryApplyXenoDebuffMultiplier(targetId, xeno.Comp.StunTime);
            _stun.TryParalyze(targetId, stunTime, true);

            var stunned = EnsureComp<XenoLungeStunnedComponent>(targetId);
            stunned.ExpireAt = _timing.CurTime + stunTime;
            stunned.Stunner = GetNetEntity(xeno);
            Dirty(targetId, stunned);
        }

        if (TryComp(xeno, out MeleeWeaponComponent? melee))
        {
            melee.NextAttack = _timing.CurTime;
            Dirty(xeno, melee);
        }

        StopLunge(xeno);
        _pulling.TryStartPull(xeno, targetId);
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

    private void OnXenoLungeHitAttempt(Entity<RMCLungeProtectionComponent> ent, ref XenoLungeHitAttempt args)
    {
        if(args.Cancelled)
            return;

        if(!TryComp(args.Lunging, out XenoLungeComponent? lunging) || lunging.Origin == null)
            return;

        args.Cancelled = _leap.AttemptBlockLeap(ent.Owner, ent.Comp.StunDuration,ent.Comp.BlockSound, args.Lunging, _transform.ToCoordinates(lunging.Origin.Value), ent.Comp.FullProtection);
    }

    private void OnAttackAttempt(Entity<XenoLungeComponent> ent, ref MeleeAttackAttemptEvent args)
    {
        var netAttacker = GetNetEntity(ent);
        if(!TryComp(GetEntity(args.Target), out XenoLungeStunnedComponent? stunned) || netAttacker != stunned.Stunner)
            return;

        switch (args.Attack)
        {
            case DisarmAttackEvent disarm:
                args.Attack = new LightAttackEvent(disarm.Target, netAttacker, disarm.Coordinates);
                break;
        }
    }

    private void StopLunge(EntityUid lunging)
    {
        if (!_physicsQuery.TryGetComponent(lunging, out var physics))
            return;

        _physics.SetLinearVelocity(lunging, Vector2.Zero, body: physics);
        _physics.SetBodyStatus(lunging, physics, BodyStatus.OnGround);
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

[ByRefEvent]
public record struct XenoLungeHitAttempt(EntityUid Lunging, bool Cancelled = false);
