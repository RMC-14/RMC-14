using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Slow;
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
    [Dependency] private readonly XenoHookSystem _hook = default!;
    [Dependency] private readonly XenoProjectileSystem _projectile = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly RMCPullingSystem _pulling = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

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
        var target = _transform.GetMoverCoordinates(args.Target);
        var diff = origin.Position - target.Position;
        if (!origin.TryDistance(EntityManager, target, out var dis))
            return;
        diff = diff.Normalized() * Math.Max(dis - 2, 0.5f); // Lands right in front

        _throwing.TryThrow(args.Target, diff, 10, user: args.Shooter);
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
        if (args.Handled || args.Coords == null)
            return;

        if (!_actionBlocker.CanAttack(xeno))
            return;

        _projectile.TryShoot(xeno, args.Coords.Value, 0, xeno.Comp.Projectile, null, 1, Angle.Zero, xeno.Comp.Speed, target: args.Entity);

        if (TryComp(xeno, out MeleeWeaponComponent? melee))
        {
            if (_timing.CurTime < melee.NextAttack)
                return;

            melee.NextAttack = _timing.CurTime + TimeSpan.FromSeconds(1);
            Dirty(xeno, melee);
        }

        var attackEv = new MeleeAttackEvent(xeno);
        RaiseLocalEvent(xeno, ref attackEv);

        _audio.PlayPredicted(xeno.Comp.Sound, xeno, xeno);

        args.Handled = true;
    }
}
