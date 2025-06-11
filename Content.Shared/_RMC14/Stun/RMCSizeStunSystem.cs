using System.Numerics;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stamina;
using Content.Shared.Coordinates;
using Content.Shared.Flash;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
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
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedFlashSystem _flash = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly RMCStaminaSystem _stamina = default!;
    [Dependency] private readonly StandingStateSystem _stand = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<Entity<MarineComponent>> _marines = new();

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

        KnockBack(args.Target, bullet.Comp.ShotFrom, bullet.Comp.KnockBackPowerMin, bullet.Comp.KnockBackPowerMax, bullet.Comp.KnockBackSpeed);

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
            _stamina.DoStaminaDamage(args.Target, args.Damage.GetTotal().Float());

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
    public void KnockBack(EntityUid target, EntityCoordinates? shotFrom, float knockBackPowerMin = 1f, float knockBackPowerMax = 1f, float knockBackSpeed = 5f)
    {
        if (!TryComp<RMCSizeComponent>(target, out var size) || size.Size >= RMCSizes.Big)
            return;

        if(shotFrom == null)
            return;

        //TODO Camera Shake
        _physics.SetLinearVelocity(target, Vector2.Zero);
        _physics.SetAngularVelocity(target, 0f);

        var vec = _transform.GetMoverCoordinates(target).Position - shotFrom.Value.Position;
        if (vec.Length() != 0)
        {
            _rmcPulling.TryStopPullsOn(target);
            var knockBackPower = _random.NextFloat(knockBackPowerMin, knockBackPowerMax);
            var direction = vec.Normalized() * knockBackPower;
            _throwing.TryThrow(target, direction, knockBackSpeed, animated: false, playSound: false, doSpin: false);
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
            KnockBack(target, ent.Comp.ShotFrom, ent.Comp.KnockBackPowerMin, ent.Comp.KnockBackPowerMax, ent.Comp.KnockBackSpeed);
            break;
        }
    }

    private void OnStunOnTrigger(Entity<RMCStunOnTriggerComponent> ent, ref RMCTriggerEvent args)
    {
        if (_net.IsClient)
            return;

        _marines.Clear();
        _entityLookup.GetEntitiesInRange(ent.Owner.ToCoordinates(), ent.Comp.Range, _marines);
        foreach (var target in _marines)
        {
            if (ent.Comp.Filters != null)
            {
                var passedFilter = false;
                foreach (var filter in ent.Comp.Filters)
                {
                    if (_entityWhitelist.IsWhitelistFail(filter.Whitelist, target))
                        continue;

                    var probability = filter.Probability ?? ent.Comp.Probability;
                    var range = filter.Range ?? ent.Comp.Range;
                    var stun = filter.Stun ?? ent.Comp.Stun;
                    var flash = filter.Flash ?? ent.Comp.Flash;
                    var flashAdditionalStunTime = filter.FlashAdditionalStunTime ?? ent.Comp.FlashAdditionalStunTime;
                    Stun(ent, target, args.User, probability, range, stun, flash, flashAdditionalStunTime);
                    passedFilter = true;
                    break;
                }

                if (passedFilter)
                    continue;
            }

            Stun(
                ent,
                target,
                args.User,
                ent.Comp.Probability,
                ent.Comp.Range,
                ent.Comp.Stun,
                ent.Comp.Flash,
                ent.Comp.FlashAdditionalStunTime
            );
        }

        args.Handled = true;
    }

    private void Stun(Entity<RMCStunOnTriggerComponent> ent, EntityUid target, EntityUid? user, float probability, float range, TimeSpan stun, TimeSpan flash, TimeSpan flashAdditionalStunTime)
    {
        var coordinates = Transform(target).Coordinates;
        if (!_random.Prob(probability) || !_interaction.InRangeUnobstructed(ent, coordinates, range))
            return;

        if (_flash.Flash(target, user, ent, (float) flash.TotalMilliseconds, displayPopup: false))
            stun += flashAdditionalStunTime;

        _stun.TryStun(target, stun, true);
        _stun.TryKnockdown(target, stun, true);
    }
}
