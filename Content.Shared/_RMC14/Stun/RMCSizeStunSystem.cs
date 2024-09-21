using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Stun;

public sealed class RMCSizeStunSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StandingStateSystem _stand = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCStunOnHitComponent, MapInitEvent>(OnSizeStunMapInit);
        SubscribeLocalEvent<RMCStunOnHitComponent, ProjectileHitEvent>(OnHit);

        SubscribeLocalEvent<AmmoSlowedComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
        SubscribeLocalEvent<AmmoSlowedComponent, ComponentRemove>(OnRemove);
    }

    private void OnRefresh(Entity<AmmoSlowedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var multiplier = (ent.Comp.SlowMultiplier * (ent.Comp.SuperSlowActive ? ent.Comp.SuperSlowMultiplier : 1)).Float();
        args.ModifySpeed(multiplier, multiplier);
    }

    private void OnRemove(Entity<AmmoSlowedComponent> ent, ref ComponentRemove args)
    {
        if (!TerminatingOrDeleted(ent))
            _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    public bool IsHumanoidSized(Entity<RMCSizeComponent> ent)
    {
        return ent.Comp.Size <= RMCSizes.Humanoid;
    }

    public bool IsXenoSized(Entity<RMCSizeComponent> ent)
    {
        return ent.Comp.Size >= RMCSizes.VerySmallXeno;
    }

    private void OnSizeStunMapInit(Entity<RMCStunOnHitComponent> projectile, ref MapInitEvent args)
    {
        projectile.Comp.ShotFrom = _transform.GetMoverCoordinates(projectile.Owner);
        Dirty(projectile);
    }

    private void OnHit(Entity<RMCStunOnHitComponent> bullet, ref ProjectileHitEvent args)
    {
        if (bullet.Comp.ShotFrom == null)
            return;

        var distance = (_transform.GetMoverCoordinates(args.Target).Position - bullet.Comp.ShotFrom.Value.Position).Length();

        if (distance > bullet.Comp.MaxRange || _stand.IsDown(args.Target))
            return;

        if (!TryComp<RMCSizeComponent>(args.Target, out var size) || size.Size >= RMCSizes.Big)
            return;

        //TODO Camera Shake

        //Knockback
        if (_blocker.CanMove(args.Target))
        {

            _physics.SetLinearVelocity(args.Target, Vector2.Zero);
            _physics.SetAngularVelocity(args.Target, 0f);

            var direction = (_transform.GetMoverCoordinates(args.Target).Position - bullet.Comp.ShotFrom.Value.Position).Normalized();

            _throwing.TryThrow(args.Target, direction, 1, animated: false, playSound: false, doSpin: false);

            // RMC-14 TODO Thrown into obstacle mechanics
        }

        //Stun part
        if (IsXenoSized((args.Target, size)))
        {
            var stun = bullet.Comp.StunTime;
            var superSlow = bullet.Comp.SuperSlowTime;
            var slow = bullet.Comp.SlowTime;

            if (bullet.Comp.LosesEffectWithRange)
            {
                stun -= TimeSpan.FromSeconds(distance / 50);
                superSlow -= TimeSpan.FromSeconds(distance / 10);
                slow -= TimeSpan.FromSeconds(distance / 5);
            }

            _stun.TryParalyze(args.Target, stun, true);

            EnsureComp<AmmoSlowedComponent>(args.Target, out var ammoSlowed);

            ammoSlowed.ExpireTime = _timing.CurTime + slow;
            ammoSlowed.SuperExpireTime = _timing.CurTime + superSlow;
            ammoSlowed.SuperSlowActive = true;

            Dirty(args.Target, ammoSlowed);

            _movementSpeed.RefreshMovementSpeedModifiers(args.Target);

            _popup.PopupEntity(Loc.GetString("rmc-xeno-stun-shaken"), args.Target, args.Target, PopupType.MediumCaution);
        }
        else
            _stamina.TakeStaminaDamage(args.Target, args.Damage.GetTotal().Float());
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;

        if (_net.IsServer)
        {
            var victimQuery = EntityQueryEnumerator<AmmoSlowedComponent>();

            while (victimQuery.MoveNext(out var uid, out var victim))
            {
                if (victim.SuperSlowActive && victim.SuperExpireTime <= time)
                {
                    victim.SuperSlowActive = false;
                    _movementSpeed.RefreshMovementSpeedModifiers(uid);
                    continue;
                }

                if (victim.ExpireTime > time)
                    continue;

                RemCompDeferred<AmmoSlowedComponent>(uid);
                _movementSpeed.RefreshMovementSpeedModifiers(uid);
            }
        }
    }
}
