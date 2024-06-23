using Content.Shared._RMC14.Xenonids.Projectile.Spit.Charge;
using Content.Shared._RMC14.Xenonids.Projectile.Spit.Scattered;
using Content.Shared._RMC14.Xenonids.Projectile.Spit.Slowing;
using Content.Shared._RMC14.Xenonids.Projectile.Spit.Standard;
using Content.Shared.Effects;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Projectile.Spit;

public sealed class XenoSpitSystem : EntitySystem
{
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoProjectileSystem _xenoProjectile = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoSpitComponent, XenoSpitActionEvent>(OnXenoSpitAction);
        SubscribeLocalEvent<XenoSlowingSpitComponent, XenoSlowingSpitActionEvent>(OnXenoSlowingSpitAction);
        SubscribeLocalEvent<XenoScatteredSpitComponent, XenoScatteredSpitActionEvent>(OnXenoScatteredSpitAction);
        SubscribeLocalEvent<XenoChargeSpitComponent, XenoChargeSpitActionEvent>(OnXenoChargeSpitAction);

        SubscribeLocalEvent<XenoSlowingSpitProjectileComponent, ProjectileHitEvent>(OnXenoSlowingSpitHit);

        SubscribeLocalEvent<SlowedBySpitComponent, RefreshMovementSpeedModifiersEvent>(OnSlowedBySpitRefreshMovement);

        SubscribeLocalEvent<InventoryComponent, HitBySlowingSpitEvent>(_inventory.RelayEvent);
    }

    private void OnXenoSpitAction(Entity<XenoSpitComponent> xeno, ref XenoSpitActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = _xenoProjectile.TryShoot(
            xeno,
            args.Target,
            xeno.Comp.PlasmaCost,
            xeno.Comp.ProjectileId,
            xeno.Comp.Sound,
            1,
            Angle.Zero,
            xeno.Comp.Speed,
            true
        );

        if (RemCompDeferred<XenoActiveChargingSpitComponent>(xeno))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-charge-spit-expire"), xeno, xeno, PopupType.SmallCaution);
        }
    }

    private void OnXenoSlowingSpitAction(Entity<XenoSlowingSpitComponent> xeno, ref XenoSlowingSpitActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = _xenoProjectile.TryShoot(
            xeno,
            args.Target,
            xeno.Comp.PlasmaCost,
            xeno.Comp.ProjectileId,
            xeno.Comp.Sound,
            1,
            Angle.Zero,
            xeno.Comp.Speed
        );
    }

    private void OnXenoScatteredSpitAction(Entity<XenoScatteredSpitComponent> xeno, ref XenoScatteredSpitActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = _xenoProjectile.TryShoot(
            xeno,
            args.Target,
            xeno.Comp.PlasmaCost,
            xeno.Comp.ProjectileId,
            xeno.Comp.Sound,
            xeno.Comp.MaxProjectiles,
            xeno.Comp.MaxDeviation,
            xeno.Comp.Speed
        );
    }

    private void OnXenoChargeSpitAction(Entity<XenoChargeSpitComponent> xeno, ref XenoChargeSpitActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var charging = EnsureComp<XenoActiveChargingSpitComponent>(xeno);
        charging.ExpiresAt = _timing.CurTime + xeno.Comp.Duration;
        charging.Damage = xeno.Comp.Damage;
        charging.ProjectileLifetime = xeno.Comp.Lifetime;
        Dirty(xeno, charging);

        _popup.PopupClient(Loc.GetString("cm-xeno-charge-spit"), xeno, xeno);
    }

    private void OnXenoSlowingSpitHit(Entity<XenoSlowingSpitProjectileComponent> spit, ref ProjectileHitEvent args)
    {
        if (_net.IsClient)
            return;

        var target = args.Target;
        if (_xenoProjectile.SameHive(spit.Owner, target))
        {
            QueueDel(spit);
            return;
        }

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
            _stun.TryParalyze(target, spit.Comp.Paralyze, true);

        _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { target }, Filter.Pvs(target));
    }

    private void OnSlowedBySpitRefreshMovement(Entity<SlowedBySpitComponent> slowed, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (slowed.Comp.ExpiresAt > _timing.CurTime)
            args.ModifySpeed(slowed.Comp.Multiplier, slowed.Comp.Multiplier);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var slowedQuery = EntityQueryEnumerator<SlowedBySpitComponent>();
        while (slowedQuery.MoveNext(out var uid, out var slowed))
        {
            if (slowed.ExpiresAt >= time)
                continue;

            RemCompDeferred<SlowedBySpitComponent>(uid);
            _movementSpeed.RefreshMovementSpeedModifiers(uid);
        }

        var chargingQuery = EntityQueryEnumerator<XenoActiveChargingSpitComponent>();
        while (chargingQuery.MoveNext(out var uid, out var charging))
        {
            if (charging.ExpiresAt >= time)
                continue;

            RemCompDeferred<XenoActiveChargingSpitComponent>(uid);
            _popup.PopupClient(Loc.GetString("cm-xeno-charge-spit-expire"), uid, uid, PopupType.SmallCaution);
        }
    }
}
