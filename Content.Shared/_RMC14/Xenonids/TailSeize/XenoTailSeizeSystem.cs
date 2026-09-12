using Content.Shared._RMC14.Damage.ObstacleSlamming;
using Content.Shared._RMC14.Fishing;
using Content.Shared._RMC14.Projectiles;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.Hook;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared.ActionBlocker;
using Content.Shared.Coordinates;
using Content.Shared.Fishing;
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
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;
    [Dependency] private readonly RMCObstacleSlammingSystem _obstacleSlamming = default!;
    [Dependency] private readonly SharedRMCFishingSystem _fishing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoTailSeizeComponent, XenoTailSeizeActionEvent>(OnTailSeizeAction);

        SubscribeLocalEvent<VictimTailSeizedComponent, StopThrowEvent>(OnSeizeEnd);

        SubscribeLocalEvent<XenoHookComponent, AmmoShotEvent>(OnHookMade);

        SubscribeLocalEvent<XenoHookOnHitComponent, ProjectileHitEvent>(OnHookHit);
        SubscribeLocalEvent<XenoHookOnHitComponent, BeforeProjectileMaxRangeDeleteEvent>(OnHookMiss);
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

        args.Handled = true;

        HookTarget(hook, args.Shooter, args.Target);
    }

    private void HookTarget(Entity<XenoHookOnHitComponent> hook, EntityUid? xeno, EntityUid hit)
    {
        if (!TryComp<XenoHookComponent>(xeno, out var hookComp))
            return;

        if (!_hook.TryHookTarget((xeno.Value, hookComp), hit))
            return;

        _pulling.TryStopAllPullsFromAndOn(hit);

        var origin = _transform.GetMoverCoordinates(xeno.Value);
        var mapCoords = _transform.GetMapCoordinates(xeno.Value);
        var target = _transform.GetMoverCoordinates(hit);
        if (!origin.TryDistance(EntityManager, target, out var dis))
            return;

        var knockBackDistance = dis < hook.Comp.TargetStopDistance
            ? -hook.Comp.MinimumHookDistance
            : -(dis - hook.Comp.TargetStopDistance);
        _obstacleSlamming.MakeImmune(hit);
        _size.KnockBack(hit, mapCoords, knockBackDistance, knockBackDistance, 10, true);
        EnsureComp<VictimTailSeizedComponent>(hit);
    }

    private void OnHookMiss(Entity<XenoHookOnHitComponent> hook, ref BeforeProjectileMaxRangeDeleteEvent args)
    {
        if (!TryComp<ProjectileComponent>(hook, out var projectile) || projectile.ProjectileSpent)
            return;

        if (projectile.Shooter != null && TryComp<XenoFishingComponent>(projectile.Shooter, out var fishing))
        {
            _fishing.DoXenoFish((projectile.Shooter.Value, fishing), Transform(hook.Owner).Coordinates, out var caught, false, false);

            if (caught != null)
                HookTarget(hook, projectile.Shooter, caught.Value);
        }
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
