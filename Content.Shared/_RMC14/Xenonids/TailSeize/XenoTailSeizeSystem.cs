using Content.Shared._RMC14.Damage.ObstacleSlamming;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.Hook;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared.ActionBlocker;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.TailSeize;

public sealed class XenoTailSeizeSystem : EntitySystem
{
    [Dependency] private XenoHookSystem _hook = default!;
    [Dependency] private XenoProjectileSystem _projectile = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private XenoSystem _xeno = default!;
    [Dependency] private RMCSlowSystem _slow = default!;
    [Dependency] private RMCPullingSystem _pulling = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private RMCSizeStunSystem _size = default!;
    [Dependency] private RMCObstacleSlammingSystem _obstacleSlamming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoTailSeizeComponent, XenoTailSeizeActionEvent>(OnTailSeizeAction);

        SubscribeLocalEvent<VictimTailSeizedComponent, StopThrowEvent>(OnSeizeEnd);

        SubscribeLocalEvent<XenoHookComponent, AmmoShotEvent>(OnHookMade);
        SubscribeLocalEvent<XenoHookOnHitComponent, ProjectileHitEvent>(OnHookHit);
    }

    private void OnHookMade(Entity<XenoHookComponent> hook, ref AmmoShotEvent args)
    {
        foreach (var shot in args.FiredProjectiles)
        {
            _hook.TryHookTarget(hook, shot);
        }
    }

    private void OnHookHit(Entity<XenoHookOnHitComponent> hook, ref ProjectileHitEvent args)
    {
        if (_net.IsClient || args.Shooter == null)
            return;

        if (!_xeno.CanAbilityAttackTarget(args.Shooter.Value, args.Target))
            return;

        if (!TryComp<XenoHookComponent>(args.Shooter, out var hookComp))
            return;

        if (!_hook.TryHookTarget((args.Shooter.Value, hookComp), args.Target))
            return;

        _pulling.TryStopAllPullsFromAndOn(args.Target);

        var origin = _transform.GetMoverCoordinates(args.Shooter.Value);
        var mapCoords = _transform.GetMapCoordinates(args.Shooter.Value);
        var target = _transform.GetMoverCoordinates(args.Target);
        if (!origin.TryDistance(EntityManager, target, out var dis))
            return;

        var knockBackDistance = dis < hook.Comp.TargetStopDistance
            ? -hook.Comp.MinimumHookDistance
            : -(dis - hook.Comp.TargetStopDistance);
        _obstacleSlamming.MakeImmune(args.Target);
        _size.KnockBack(args.Target, mapCoords, knockBackDistance, knockBackDistance, 10);
        EnsureComp<VictimTailSeizedComponent>(args.Target);
    }

    private void OnSeizeEnd(Entity<VictimTailSeizedComponent> victim, ref StopThrowEvent args)
    {
        _slow.TrySlowdown(victim, victim.Comp.SlowTime, ignoreDurationModifier: true);
        _slow.TryRoot(victim, victim.Comp.RootTime);
        RemCompDeferred<VictimTailSeizedComponent>(victim);
    }

    private void OnTailSeizeAction(Entity<XenoTailSeizeComponent> xeno, ref XenoTailSeizeActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_actionBlocker.CanAttack(xeno))
            return;

        if (TryComp(xeno, out MeleeWeaponComponent? melee))
        {
            if (_timing.CurTime < melee.NextAttack)
                return;

            melee.NextAttack = _timing.CurTime + TimeSpan.FromSeconds(1);
            Dirty(xeno, melee);
        }

        _projectile.TryShoot(xeno, args.Target, 0, xeno.Comp.Projectile, null, 1, Angle.Zero, xeno.Comp.Speed, target: args.Entity);

        var attackEv = new MeleeAttackEvent(xeno);
        RaiseLocalEvent(xeno, ref attackEv);

        _audio.PlayPredicted(xeno.Comp.Sound, xeno, xeno);

        args.Handled = true;
    }
}
