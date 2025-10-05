using System.Numerics;
using Content.Shared._RMC14.Deafness;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stamina;
using Content.Shared.Coordinates;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Flash;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Speech.Muting;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Content.Shared.Pointing;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Stun;

public sealed class RMCSizeStunSystem : EntitySystem
{
    private const double DazedMultiplierSmallXeno = 0.7;
    private const double DazedMultiplierBigXeno = 1.2;
    private static readonly ProtoId<StatusEffectPrototype> KnockedOut = "Unconscious";

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
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly HashSet<Entity<MarineComponent>> _marines = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCStunOnHitComponent, MapInitEvent>(OnSizeStunMapInit);
        SubscribeLocalEvent<RMCStunOnHitComponent, ProjectileHitEvent>(OnHit);
        SubscribeLocalEvent<RMCStunOnHitComponent, RMCTriggerEvent>(OnTrigger);

        SubscribeLocalEvent<RMCStunOnTriggerComponent, RMCTriggerEvent>(OnStunOnTrigger);

        SubscribeLocalEvent<RMCUnconsciousComponent, ComponentStartup>(OnUnconsciousStart);
        SubscribeLocalEvent<RMCUnconsciousComponent, ComponentShutdown>(OnUnconsciousEnd);
        SubscribeLocalEvent<RMCUnconsciousComponent, StatusEffectEndedEvent>(OnUnconsciousUpdate);
        SubscribeLocalEvent<RMCUnconsciousComponent, PointAttemptEvent>(OnUnconsciousPointAttempt);

        SubscribeLocalEvent<RMCKnockOutOnCollideComponent, ProjectileHitEvent>(OnKnockOutCollideProjectileHit);
        SubscribeLocalEvent<RMCKnockOutOnCollideComponent, ThrowDoHitEvent>(OnKnockOutCollideThrowHit);
    }

    public bool IsHumanoidSized(Entity<RMCSizeComponent> ent)
    {
        return ent.Comp.Size <= RMCSizes.Humanoid;
    }
    public bool IsHumanoidSized(RMCSizes size)
    {
        return size <= RMCSizes.Humanoid;
    }

    public bool IsXenoSized(Entity<RMCSizeComponent> ent)
    {
        return ent.Comp.Size >= RMCSizes.VerySmallXeno;
    }
    public bool IsXenoSized(RMCSizes size)
    {
        return size >= RMCSizes.VerySmallXeno;
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
        projectile.Comp.ShotFrom = _transform.GetMapCoordinates(projectile.Owner);
        Dirty(projectile);
    }

    private void OnHit(Entity<RMCStunOnHitComponent> bullet, ref ProjectileHitEvent args)
    {
        if (bullet.Comp.ShotFrom == null)
            return;

        var distance = (_transform.GetMoverCoordinates(args.Target).Position - bullet.Comp.ShotFrom.Value.Position).Length();

        if (distance > bullet.Comp.MaxRange || _stand.IsDown(args.Target))
            return;

        if (!TryComp<RMCSizeComponent>(args.Target, out var size))
            return;

        KnockBack(args.Target, bullet.Comp.ShotFrom, bullet.Comp.KnockBackPowerMin, bullet.Comp.KnockBackPowerMax, bullet.Comp.KnockBackSpeed);

        if (_net.IsClient)
            return;

        // Multiply daze duration based on the size of the target
        var dazeMultiplier = 1.0;
        if (size.Size >= RMCSizes.Big)
            dazeMultiplier = DazedMultiplierBigXeno;
        else if (size.Size <= RMCSizes.SmallXeno && IsXenoSized((args.Target, size)))
            dazeMultiplier = DazedMultiplierSmallXeno;

        //Try to daze before the big size check, because big xenos can still be dazed.
        _dazed.TryDaze(args.Target, bullet.Comp.DazeTime * dazeMultiplier);

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
    public void KnockBack(EntityUid target, MapCoordinates? knockedBackFrom, float knockBackPowerMin = 1f, float knockBackPowerMax = 1f, float knockBackSpeed = 5f, bool ignoreSize = false)
    {
        if ((!TryComp<RMCSizeComponent>(target, out var size) || size.Size >= RMCSizes.Big) && !ignoreSize)
            return;

        if (knockedBackFrom == null)
            return;

        //TODO Camera Shake
        _physics.SetLinearVelocity(target, Vector2.Zero);
        _physics.SetAngularVelocity(target, 0f);

        var vec = _transform.GetMoverCoordinates(target).Position - knockedBackFrom.Value.Position;
        if (vec.Length() != 0)
        {
            _rmcPulling.TryStopPullsOn(target);
            var knockBackPower = _random.NextFloat(knockBackPowerMin, knockBackPowerMax);
            var direction = vec.Normalized() * knockBackPower;
            _throwing.TryThrow(target, direction, knockBackSpeed, animated: false, playSound: false, compensateFriction: true);
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
                    var paralyze = filter.Paralyze ?? ent.Comp.Paralyze;
                    Stun(ent, target, args.User, probability, range, stun, flash, flashAdditionalStunTime, paralyze);
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
                ent.Comp.FlashAdditionalStunTime,
                ent.Comp.Paralyze
            );
        }

        args.Handled = true;
    }

    private void Stun(Entity<RMCStunOnTriggerComponent> ent, EntityUid target, EntityUid? user, float probability, float range, TimeSpan stun, TimeSpan flash, TimeSpan flashAdditionalStunTime, TimeSpan paralyze)
    {
        var coordinates = Transform(target).Coordinates;
        if (!_random.Prob(probability) || !_interaction.InRangeUnobstructed(ent, coordinates, range))
            return;

        if (_flash.Flash(target, user, ent, (float)flash.TotalMilliseconds, displayPopup: false))
        {
            stun += flashAdditionalStunTime;
            paralyze += flashAdditionalStunTime;
        }

        if (stun > TimeSpan.Zero)
        {
            _stun.TryStun(target, stun, true);
            _stun.TryKnockdown(target, stun, true);
        }

        if (paralyze > TimeSpan.Zero)
        {
            TryKnockOut(target, paralyze, true);
        }
    }

    //Equal to KnockOut/PARALYZE in parity
    public bool TryKnockOut(EntityUid uid, TimeSpan duration, bool refresh = true, StatusEffectsComponent? status = null)
    {
        if (duration <= TimeSpan.Zero)
            return false;

        if (!Resolve(uid, ref status, false))
            return false;

        if (!_status.TryAddStatusEffect<RMCUnconsciousComponent>(uid, KnockedOut, duration, refresh))
            return false;

        return true;
    }

    private void OnUnconsciousStart(Entity<RMCUnconsciousComponent> ent, ref ComponentStartup args)
    {
        //Applies stun, knockdown, blind, deafen, and mute
        //Note applies comps directly to not mess with other status effect timers
        EnsureComp<StunnedComponent>(ent);
        EnsureComp<KnockedDownComponent>(ent);
        EnsureComp<TemporaryBlindnessComponent>(ent);
        EnsureComp<MutedComponent>(ent);
        EnsureComp<DeafComponent>(ent);
    }

    private void OnUnconsciousEnd(Entity<RMCUnconsciousComponent> ent, ref ComponentShutdown args)
    {
        var time = _timing.CurTime;
        if (!_status.TryGetTime(ent, "Stun", out var statusTime) || statusTime.Value.Item2 < time)
            RemCompDeferred<StunnedComponent>(ent);
        if (!_status.TryGetTime(ent, "KnockedDown", out statusTime) || statusTime.Value.Item2 < time)
            RemCompDeferred<KnockedDownComponent>(ent);
        if (!_status.TryGetTime(ent, "TemporaryBlindness", out statusTime) || statusTime.Value.Item2 < time)
            RemCompDeferred<TemporaryBlindnessComponent>(ent);
        if (!_status.TryGetTime(ent, "Muted", out statusTime) || statusTime.Value.Item2 < time)
            RemCompDeferred<MutedComponent>(ent);
        if (!_status.TryGetTime(ent, "Deaf", out statusTime) || statusTime.Value.Item2 < time)
            RemCompDeferred<DeafComponent>(ent);
    }

    private void OnUnconsciousUpdate(Entity<RMCUnconsciousComponent> ent, ref StatusEffectEndedEvent args)
    {
        if (!IsKnockedOut(ent))
            return;

        //Readd comps just in case they were removed by a status
        EnsureComp<StunnedComponent>(ent);
        EnsureComp<KnockedDownComponent>(ent);
        EnsureComp<TemporaryBlindnessComponent>(ent);
        EnsureComp<MutedComponent>(ent);
        EnsureComp<DeafComponent>(ent);
    }

    private void OnUnconsciousPointAttempt(Entity<RMCUnconsciousComponent> ent, ref PointAttemptEvent args)
    {
        if (!IsKnockedOut(ent))
            return;

        args.Cancel();
    }

    private void OnKnockOutCollideProjectileHit(Entity<RMCKnockOutOnCollideComponent> ent, ref ProjectileHitEvent args)
    {
        TryKnockOut(args.Target, ent.Comp.ParalyzeTime);
    }

    private void OnKnockOutCollideThrowHit(Entity<RMCKnockOutOnCollideComponent> ent, ref ThrowDoHitEvent args)
    {
        TryKnockOut(args.Target, ent.Comp.ParalyzeTime);
    }

    public bool IsKnockedOut(EntityUid uid)
    {
        return _status.HasStatusEffect(uid, KnockedOut);
    }
}
