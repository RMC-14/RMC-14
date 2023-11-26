using Content.Shared._CM14.Xenos.Plasma;
using Content.Shared._CM14.Xenos.Spit.Scattered;
using Content.Shared._CM14.Xenos.Spit.Slowing;
using Content.Shared.Armor;
using Content.Shared.Effects;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos.Spit;

public abstract class SharedXenoSpitSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoSlowingSpitComponent, XenoSlowingSpitActionEvent>(OnXenoSlowingSpitAction);
        SubscribeLocalEvent<XenoScatteredSpitComponent, XenoScatteredSpitActionEvent>(OnXenoScatteredSpitAction);

        SubscribeLocalEvent<XenoSlowingSpitProjectileComponent, PreventCollideEvent>(OnXenoSlowingSpitPreventCollide);
        SubscribeLocalEvent<XenoSlowingSpitProjectileComponent, ProjectileHitEvent>(OnXenoSlowingSpitHit);

        SubscribeLocalEvent<SlowedBySpitComponent, RefreshMovementSpeedModifiersEvent>(OnSlowedBySpitRefreshMovement);
        SubscribeLocalEvent<SlowedBySpitComponent, EntityUnpausedEvent>(OnSlowedBySpitUnpaused);

        SubscribeLocalEvent<ArmorComponent, InventoryRelayedEvent<HitBySlowingSpitEvent>>(OnArmorHitBySlowingSpit);
    }

    // TODO CM14 merge this and scattered spit and add a range limit of 6 tiles
    private void OnXenoSlowingSpitAction(Entity<XenoSlowingSpitComponent> xeno, ref XenoSlowingSpitActionEvent args)
    {
        if (args.Handled)
            return;

        var origin = _transform.GetMapCoordinates(xeno);
        var target = args.Target.ToMap(EntityManager, _transform);

        if (origin.MapId != target.MapId ||
            origin.Position == target.Position)
        {
            return;
        }

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        args.Handled = true;

        _audio.PlayPredicted(xeno.Comp.Sound, xeno, xeno);
        Shoot(xeno, xeno.Comp.ProjectileId, xeno.Comp.Speed, origin, target);
    }

    private void OnXenoScatteredSpitAction(Entity<XenoScatteredSpitComponent> xeno, ref XenoScatteredSpitActionEvent args)
    {
        if (args.Handled)
            return;

        var origin = _transform.GetMapCoordinates(xeno);
        var target = args.Target.ToMap(EntityManager, _transform);

        if (origin.MapId != target.MapId ||
            origin.Position == target.Position)
        {
            return;
        }

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        args.Handled = true;

        _audio.PlayPredicted(xeno.Comp.Sound, xeno, xeno);

        var diff = target.Position - origin.Position;

        for (var i = 0; i < xeno.Comp.MaxProjectiles; i++)
        {
            var angle = _random.NextAngle(-xeno.Comp.MaxDeviation / 2, xeno.Comp.MaxDeviation / 2);
            target = new MapCoordinates(origin.Position + angle.RotateVec(diff), target.MapId);
            Shoot(xeno, xeno.Comp.ProjectileId, xeno.Comp.Speed, origin, target);
        }
    }

    private void OnXenoSlowingSpitPreventCollide(Entity<XenoSlowingSpitProjectileComponent> spit, ref PreventCollideEvent args)
    {
        if (HasComp<XenoComponent>(args.OtherEntity))
            args.Cancelled = true;
    }

    private void OnXenoSlowingSpitHit(Entity<XenoSlowingSpitProjectileComponent> spit, ref ProjectileHitEvent args)
    {
        if (_net.IsClient)
            return;

        var target = args.Target;

        if (spit.Comp.Slow > TimeSpan.Zero)
        {
            EnsureComp<SlowedBySpitComponent>(target).ExpiresAt = _timing.CurTime + spit.Comp.Slow;
            _movementSpeed.RefreshMovementSpeedModifiers(target);
        }

        var resisted = false;
        if (spit.Comp.ArmorResistsKnockdown)
        {
            var ev = new HitBySlowingSpitEvent(SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING);
            RaiseLocalEvent(args.Target, ref ev);
            resisted = ev.Cancelled;
        }

        if (!resisted)
        {
            _stun.TryKnockdown(target, spit.Comp.Knockdown, true);
            _stun.TryStun(target, spit.Comp.Knockdown, true);
        }

        _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { target }, Filter.Pvs(target));
    }

    private void OnSlowedBySpitRefreshMovement(Entity<SlowedBySpitComponent> slowed, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (slowed.Comp.ExpiresAt > _timing.CurTime)
            args.ModifySpeed(slowed.Comp.Multiplier, slowed.Comp.Multiplier);
    }

    private void OnSlowedBySpitUnpaused(Entity<SlowedBySpitComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.ExpiresAt += args.PausedTime;
    }

    private void OnArmorHitBySlowingSpit(Entity<ArmorComponent> ent, ref InventoryRelayedEvent<HitBySlowingSpitEvent> args)
    {
        args.Args.Cancelled = true;
    }

    protected virtual void Shoot(EntityUid xeno, EntProtoId projectileId, float speed, MapCoordinates origin, MapCoordinates target)
    {
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<SlowedBySpitComponent>();
        while (query.MoveNext(out var uid, out var slowed))
        {
            if (slowed.ExpiresAt > time)
                continue;

            RemCompDeferred<SlowedBySpitComponent>(uid);
            _movementSpeed.RefreshMovementSpeedModifiers(uid);
        }
    }
}
