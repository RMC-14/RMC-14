using Content.Shared._RMC14.Xenonids.Animation;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Headbutt;

public sealed class XenoHeadbuttSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ThrownItemSystem _thrownItem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoAnimationsSystem _xenoAnimations = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<ThrownItemComponent> _thrownItemQuery;

    public override void Initialize()
    {
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _thrownItemQuery = GetEntityQuery<ThrownItemComponent>();

        SubscribeLocalEvent<XenoHeadbuttComponent, XenoHeadbuttActionEvent>(OnXenoHeadbuttAction);
        SubscribeLocalEvent<XenoHeadbuttComponent, ThrowDoHitEvent>(OnXenoHeadbuttHit);
    }

    private void OnXenoHeadbuttAction(Entity<XenoHeadbuttComponent> xeno, ref XenoHeadbuttActionEvent args)
    {
        if (!_xeno.CanAbilityAttackTarget(xeno, args.Target))
            return;

        if (args.Handled)
            return;

        var attempt = new XenoHeadbuttAttemptEvent();
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        if (TryComp(xeno, out PullerComponent? puller) && TryComp(puller.Pulling, out PullableComponent? pullable))
            _pulling.TryStopPull(puller.Pulling.Value, pullable, xeno);

        args.Handled = true;

        var origin = _transform.GetMapCoordinates(xeno);
        var target = _transform.GetMapCoordinates(args.Target);
        var diff = target.Position - origin.Position;
        var length = diff.Length();
        diff *= xeno.Comp.Range / length;

        xeno.Comp.Charge = diff;
        Dirty(xeno);

        _throwing.TryThrow(xeno, diff, 10);
    }

    private void OnXenoHeadbuttHit(Entity<XenoHeadbuttComponent> xeno, ref ThrowDoHitEvent args)
    {
        // TODO RMC14 lag compensation
        var targetId = args.Target;
        if (_physicsQuery.TryGetComponent(xeno, out var physics) &&
            _thrownItemQuery.TryGetComponent(xeno, out var thrown))
        {
            _thrownItem.LandComponent(xeno, thrown, physics, true);
            _thrownItem.StopThrow(xeno, thrown);
        }

        if (_timing.IsFirstTimePredicted && xeno.Comp.Charge is { } charge)
        {
            xeno.Comp.Charge = null;
            _xenoAnimations.PlayLungeAnimationEvent(xeno, charge);
        }

        if (_net.IsServer)
            _audio.PlayPvs(xeno.Comp.Sound, xeno);

        if (_hive.FromSameHive(xeno.Owner, targetId))
            return;

        var damage = _damageable.TryChangeDamage(targetId, xeno.Comp.Damage);
        if (damage?.GetTotal() > FixedPoint2.Zero)
        {
            var filter = Filter.Pvs(targetId, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
            _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { targetId }, filter);
        }

        var origin = _transform.GetMapCoordinates(xeno);
        var target = _transform.GetMapCoordinates(targetId);
        var diff = target.Position - origin.Position;
        var length = diff.Length();
        diff *= xeno.Comp.Range / 3 / length;

        _throwing.TryThrow(targetId, diff, 10);

        if (_net.IsServer)
            SpawnAttachedTo(xeno.Comp.Effect, targetId.ToCoordinates());
    }
}
