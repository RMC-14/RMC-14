using System.Numerics;
using Content.Shared._CM14.Marines;
using Content.Shared._CM14.Xenos.Plasma;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos.Headbutt;

public abstract class SharedXenoHeadbuttSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ThrownItemSystem _thrownItem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    private EntityQuery<MarineComponent> _marineQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<ThrownItemComponent> _thrownItemQuery;

    public override void Initialize()
    {
        _marineQuery = GetEntityQuery<MarineComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _thrownItemQuery = GetEntityQuery<ThrownItemComponent>();

        SubscribeLocalEvent<XenoHeadbuttComponent, XenoHeadbuttActionEvent>(OnXenoHeadbuttAction);
        SubscribeLocalEvent<XenoHeadbuttComponent, ThrowDoHitEvent>(OnXenoHeadbuttHit);
    }

    private void OnXenoHeadbuttAction(Entity<XenoHeadbuttComponent> xeno, ref XenoHeadbuttActionEvent args)
    {
        if (args.Handled)
            return;

        var attempt = new XenoHeadbuttAttemptEvent();
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        args.Handled = true;

        var origin = _transform.GetMapCoordinates(xeno);
        var target = args.Target.ToMap(EntityManager, _transform);
        var diff = target.Position - origin.Position;
        var length = diff.Length();
        diff *= xeno.Comp.Range / length;

        xeno.Comp.Charge = diff;
        Dirty(xeno);

        _throwing.TryThrow(xeno, diff, 10);
    }

    private void OnXenoHeadbuttHit(Entity<XenoHeadbuttComponent> xeno, ref ThrowDoHitEvent args)
    {
        // TODO CM14 lag compensation
        var marineId = args.Target;
        if (!_marineQuery.HasComponent(marineId))
        {
            return;
        }

        if (_physicsQuery.TryGetComponent(xeno, out var physics) &&
            _thrownItemQuery.TryGetComponent(xeno, out var thrown))
        {
            _thrownItem.LandComponent(xeno, thrown, physics, true);
            _thrownItem.StopThrow(xeno, thrown);
        }

        var damage = _damageable.TryChangeDamage(marineId, xeno.Comp.Damage);
        if (damage?.GetTotal() > FixedPoint2.Zero)
        {
            var filter = Filter.Pvs(marineId, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
            _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { marineId }, filter);
        }

        var origin = _transform.GetMapCoordinates(xeno);
        var target = _transform.GetMapCoordinates(marineId);
        var diff = target.Position - origin.Position;
        var length = diff.Length();
        diff *= xeno.Comp.Range / 3 / length;

        _throwing.TryThrow(marineId, diff, 10);

        if (_timing.IsFirstTimePredicted && xeno.Comp.Charge is { } charge)
        {
            xeno.Comp.Charge = null;
            DoLunge(xeno, charge.Normalized());
        }

        if (_net.IsClient)
            return;

        _audio.PlayPvs(xeno.Comp.Sound, xeno);
        SpawnAttachedTo(xeno.Comp.Effect, marineId.ToCoordinates());
    }

    protected virtual void DoLunge(EntityUid xeno, Vector2 direction)
    {
    }
}
