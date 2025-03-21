using System.Numerics;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Stun;

public sealed class RMCSizeStunSystem : EntitySystem
{
    private const double DazedMultiplierSmallXeno = 0.7;
    private const double DazedMultiplierBigXeno = 1.2;

    [Dependency] private readonly RMCDazedSystem _dazed = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly StandingStateSystem _stand = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCStunOnHitComponent, MapInitEvent>(OnSizeStunMapInit);
        SubscribeLocalEvent<RMCStunOnHitComponent, ProjectileHitEvent>(OnHit);
        SubscribeLocalEvent<RMCStunOnHitComponent, RMCTriggerEvent>(OnTrigger);

        SubscribeLocalEvent<RMCStunOnTriggerComponent, RMCTriggerEvent>(OnStunOnTrigger);
    }

    public bool IsHumanoidSized(Entity<RMCSizeComponent> ent)
    {
        return ent.Comp.Size <= RMCSizes.Humanoid;
    }

    public bool IsXenoSized(Entity<RMCSizeComponent> ent)
    {
        return ent.Comp.Size >= RMCSizes.VerySmallXeno;
    }

    public bool TryGetSize(EntityUid ent, out RMCSizes size)
    {
        size = default;
        if (!TryComp(ent, out RMCSizeComponent? sizeComp))
            return false;

        size = sizeComp.Size;
        return true;
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


        // Multiply daze duration based on the size of the target
        var dazeMultiplier = 1.0;
        if(TryComp(args.Target, out RMCSizeComponent? targetSize))
        {
            if (targetSize.Size >= RMCSizes.Big)
                dazeMultiplier = DazedMultiplierBigXeno;
            else if (targetSize.Size <= RMCSizes.SmallXeno && IsXenoSized((args.Target, targetSize)))
                dazeMultiplier = DazedMultiplierSmallXeno;
        }

        //Try to daze before the big size check, because big xenos can still be dazed.
        _dazed.TryDaze(args.Target, bullet.Comp.DazeTime * dazeMultiplier);

        if (!TryComp<RMCSizeComponent>(args.Target, out var size))
            return;

        KnockBack(args.Target, bullet);

        if (_net.IsClient)
            return;

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

            if (bullet.Comp.SlowsEffectBigXenos || size.Size < RMCSizes.Big)
                ApplyEffects(args.Target, stun, slow, superSlow);

            _popup.PopupEntity(Loc.GetString("rmc-xeno-stun-shaken"), args.Target, args.Target, PopupType.MediumCaution);
        }
        else
            _stamina.TakeStaminaDamage(args.Target, args.Damage.GetTotal().Float());

    }

    /// <summary>
    ///     Applies the effects from the component
    /// </summary>
    private void ApplyEffects(EntityUid uid, TimeSpan stun, TimeSpan slow, TimeSpan superSlow)
    {
        _slow.TrySlowdown(uid, slow);
        _slow.TrySuperSlowdown(uid, superSlow);

        // Don't paralyze if big
        if (!TryComp<RMCSizeComponent>(uid, out var size) || size.Size >= RMCSizes.Big)
            return;

        _stun.TryParalyze(uid, stun, true);
    }

    /// <summary>
    ///     Tries to knock back the target.
    /// </summary>
    private void KnockBack(EntityUid target, Entity<RMCStunOnHitComponent> bullet)
    {
        if (!TryComp<RMCSizeComponent>(target, out var size) || size.Size >= RMCSizes.Big)
            return;

        if(bullet.Comp.ShotFrom == null)
            return;

        //TODO Camera Shake
        _physics.SetLinearVelocity(target, Vector2.Zero);
        _physics.SetAngularVelocity(target, 0f);

        var vec = _transform.GetMoverCoordinates(target).Position - bullet.Comp.ShotFrom.Value.Position;
        if (vec.Length() != 0)
        {
            _rmcPulling.TryStopPullsOn(target);
            var knockBackPower = _random.NextFloat(bullet.Comp.KnockBackPowerMin, bullet.Comp.KnockBackPowerMax);
            var direction = vec.Normalized() * knockBackPower;
            _throwing.TryThrow(target, direction, bullet.Comp.KnockBackSpeed, animated: false, playSound: false, doSpin: false);
            // RMC-14 TODO Thrown into obstacle mechanics
        }
    }

    /// <summary>
    ///     Tries to stun a target near the entity when it is triggered.
    /// </summary>
    private void OnTrigger(Entity<RMCStunOnHitComponent> ent, ref RMCTriggerEvent args)
    {
        var moverCoordinates = _transform.GetMoverCoordinates(ent, Transform(ent));

        var location = _entityLookup.GetEntitiesInRange<StatusEffectsComponent>(moverCoordinates, ent.Comp.StunArea);

        foreach (var target in location)
        {
            ApplyEffects(target, ent.Comp.StunTime, ent.Comp.SlowTime, ent.Comp.SuperSlowTime);
            KnockBack(target, ent);
            break;
        }
    }

    private void OnStunOnTrigger(Entity<RMCStunOnTriggerComponent> ent, ref RMCTriggerEvent args)
    {
        if (_net.IsClient)
            return;

        var query = _entityLookup.GetEntitiesInRange(ent, ent.Comp.Range);
        var stunTime = TimeSpan.FromSeconds(ent.Comp.Duration);

        foreach (var entity in query)
        {
            if (HasComp<XenoComponent>(entity))
                continue;

            var transform = Transform(entity);
            if (!_random.Prob(ent.Comp.Probability) || !_interaction.InRangeUnobstructed(ent, transform.Coordinates, ent.Comp.Range))
                continue;

            _stun.TryStun(entity, stunTime, true);
            _stun.TryKnockdown(entity, stunTime, true);
        }

        args.Handled = true;
    }
}
