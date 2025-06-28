using Content.Shared._RMC14.Damage.ObstacleSlamming;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Sweep;

public sealed class XenoTailSweepSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RotateToFaceSystem _rotateTo = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;
    [Dependency] private readonly RMCObstacleSlammingSystem _obstacleSlamming = default!;

    private readonly HashSet<Entity<MobStateComponent>> _hit = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoTailSweepComponent, XenoTailSweepActionEvent>(OnXenoTailSweepAction);
    }

    private void OnXenoTailSweepAction(Entity<XenoTailSweepComponent> xeno, ref XenoTailSweepActionEvent args)
    {
        // TODO RMC14 lag compensation
        if (!TryComp(xeno, out TransformComponent? transform))
            return;

        var ev = new XenoTailSweepAttemptEvent();
        RaiseLocalEvent(xeno, ref ev);

        if (ev.Cancelled)
            return;

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        args.Handled = true;

        _audio.PlayPredicted(xeno.Comp.Sound, xeno, xeno);
        EnsureComp<XenoSweepingComponent>(xeno);

        if (_net.IsClient)
            return;

        _hit.Clear();
        _entityLookup.GetEntitiesInRange(transform.Coordinates, xeno.Comp.Range, _hit);

        var origin = _transform.GetMapCoordinates(xeno);
        foreach (var mob in _hit)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, mob))
                continue;

            if (!_interact.InRangeUnobstructed(xeno.Owner, mob.Owner, xeno.Comp.Range))
                continue;

            _rmcPulling.TryStopAllPullsFromAndOn(mob);

            if (xeno.Comp.Damage is { } damage)
                _damageable.TryChangeDamage(mob, _xeno.TryApplyXenoSlashDamageMultiplier(mob, damage), origin: xeno, tool: xeno);

            var filter = Filter.Pvs(mob, entityManager: EntityManager);
            _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { mob }, filter);

            _obstacleSlamming.MakeImmune(mob);
            _size.KnockBack(mob, origin, xeno.Comp.KnockBackDistance, xeno.Comp.KnockBackDistance);
            if (!_size.TryGetSize(mob, out var size) || size < RMCSizes.Big)
                _stun.TryParalyze(mob, _xeno.TryApplyXenoDebuffMultiplier(mob, xeno.Comp.ParalyzeTime), true);

            _audio.PlayPvs(xeno.Comp.HitSound, mob);
            SpawnAttachedTo(xeno.Comp.HitEffect, mob.Owner.ToCoordinates());
        }
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<XenoSweepingComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var sweeping, out var xform))
        {
            if (sweeping.NextRotation > _timing.CurTime)
                continue;

            if (sweeping.TotalRotations >= sweeping.MaxRotations)
            {
                RemCompDeferred<XenoSweepingComponent>(uid);
                continue;
            }

            sweeping.TotalRotations++;
            sweeping.NextRotation = _timing.CurTime + sweeping.Delay;
            sweeping.LastDirection ??= _transform.GetWorldRotation(xform).GetDir();

            var nextAngle = sweeping.LastDirection.Value.ToAngle() + Angle.FromDegrees(90);
            sweeping.LastDirection = nextAngle.GetDir();

            Dirty(uid, sweeping);

            _rotateTo.TryFaceAngle(uid, nextAngle, xform);
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<XenoSweepingComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var sweeping, out var xform))
        {
            if (sweeping.LastDirection is not { } direction)
                continue;

            _rotateTo.TryFaceAngle(uid, direction.ToAngle(), xform);
        }
    }
}
