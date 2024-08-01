using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Xenonids.Projectile.Spit.Charge;
using Content.Shared._RMC14.Xenonids.Projectile.Spit.Scattered;
using Content.Shared._RMC14.Xenonids.Projectile.Spit.Slowing;
using Content.Shared._RMC14.Xenonids.Projectile.Spit.Standard;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
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
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
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

        SubscribeLocalEvent<XenoActiveChargingSpitComponent, ComponentRemove>(OnActiveChargingSpitRemove);
        SubscribeLocalEvent<XenoActiveChargingSpitComponent, CMGetArmorEvent>(OnActiveChargingSpitGetArmor);
        SubscribeLocalEvent<XenoActiveChargingSpitComponent, RefreshMovementSpeedModifiersEvent>(OnActiveChargingSpitRefreshSpeed);
        SubscribeLocalEvent<XenoActiveChargingSpitComponent, XenoGetSpitProjectileEvent>(OnActiveChargingSpitGetProjectile);

        SubscribeLocalEvent<XenoSlowingSpitProjectileComponent, ProjectileHitEvent>(OnXenoSlowingSpitHit);

        SubscribeLocalEvent<SlowedBySpitComponent, RefreshMovementSpeedModifiersEvent>(OnSlowedBySpitRefreshMovement);

        SubscribeLocalEvent<UserAcidedComponent, MapInitEvent>(OnUserAcidedMapInit);
        SubscribeLocalEvent<UserAcidedComponent, ComponentRemove>(OnUserAcidedRemove);
        SubscribeLocalEvent<UserAcidedComponent, ShowFireAlertEvent>(OnUserAcidedShowFireAlert);

        SubscribeLocalEvent<InventoryComponent, HitBySlowingSpitEvent>(_inventory.RelayEvent);
    }

    private void OnActiveChargingSpitRemove(Entity<XenoActiveChargingSpitComponent> ent, ref ComponentRemove args)
    {
        if (!TerminatingOrDeleted(ent))
            _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnActiveChargingSpitGetArmor(Entity<XenoActiveChargingSpitComponent> ent, ref CMGetArmorEvent args)
    {
        args.Armor += ent.Comp.Armor;
    }

    private void OnActiveChargingSpitRefreshSpeed(Entity<XenoActiveChargingSpitComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.Speed, ent.Comp.Speed);
    }

    private void OnActiveChargingSpitGetProjectile(Entity<XenoActiveChargingSpitComponent> ent, ref XenoGetSpitProjectileEvent args)
    {
        args.Id = ent.Comp.Projectile;
    }

    private void OnXenoSpitAction(Entity<XenoSpitComponent> xeno, ref XenoSpitActionEvent args)
    {
        if (args.Handled)
            return;

        var ev = new XenoGetSpitProjectileEvent(xeno.Comp.ProjectileId);
        RaiseLocalEvent(xeno, ref ev);

        args.Handled = _xenoProjectile.TryShoot(
            xeno,
            args.Target,
            xeno.Comp.PlasmaCost,
            ev.Id,
            xeno.Comp.Sound,
            1,
            Angle.Zero,
            xeno.Comp.Speed
        );

        if (RemCompDeferred<XenoActiveChargingSpitComponent>(xeno))
            _popup.PopupClient(Loc.GetString("cm-xeno-charge-spit-expire"), xeno, xeno, PopupType.SmallCaution);
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
        charging.Armor = xeno.Comp.Armor;
        charging.Speed = xeno.Comp.Speed;
        Dirty(xeno, charging);

        _movementSpeed.RefreshMovementSpeedModifiers(xeno);

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

    private void OnUserAcidedMapInit(Entity<UserAcidedComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.ExpiresAt = _timing.CurTime + ent.Comp.Duration;
        Dirty(ent);
        UpdateAppearance(ent);
    }

    private void OnUserAcidedRemove(Entity<UserAcidedComponent> ent, ref ComponentRemove args)
    {
        _appearance.SetData(ent, UserAcidedVisuals.Acided, UserAcidedEffects.None);
    }

    private void OnUserAcidedShowFireAlert(Entity<UserAcidedComponent> ent, ref ShowFireAlertEvent args)
    {
        args.Show = true;
    }

    public void SetAcidCombo(Entity<UserAcidedComponent?> acided, TimeSpan duration, DamageSpecifier? damage, TimeSpan paralyze)
    {
        if (!Resolve(acided, ref acided.Comp, false))
            return;

        if (acided.Comp.Combo)
            return;

        acided.Comp.Combo = true;

        if (damage != null)
            acided.Comp.Damage = damage;

        if (duration != default)
        {
            var oldDuration = acided.Comp.Duration;
            acided.Comp.Duration = duration;
            acided.Comp.ExpiresAt = acided.Comp.ExpiresAt - oldDuration + duration;
        }

        if (paralyze != default)
            _stun.TryParalyze(acided.Owner, paralyze, true);

        Dirty(acided);
        UpdateAppearance((acided, acided.Comp));
    }

    private void UpdateAppearance(Entity<UserAcidedComponent> acided)
    {
        var effect = acided.Comp.Combo ? UserAcidedEffects.Enhanced : UserAcidedEffects.Normal;
        _appearance.SetData(acided, UserAcidedVisuals.Acided, effect);
    }

    public void Resist(Entity<UserAcidedComponent?> player)
    {
        if (!Resolve(player, ref player.Comp, false))
            return;

        if (!_actionBlocker.CanInteract(player, null))
            return;

        _popup.PopupEntity(Loc.GetString("rmc-acid-resist"), player, player);
        _stun.TryParalyze(player.Owner, player.Comp.ResistDuration, true);
        RemCompDeferred<UserAcidedComponent>(player);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var slowedQuery = EntityQueryEnumerator<SlowedBySpitComponent>();
        while (slowedQuery.MoveNext(out var uid, out var slowed))
        {
            if (slowed.ExpiresAt > time)
                continue;

            RemCompDeferred<SlowedBySpitComponent>(uid);
            _movementSpeed.RefreshMovementSpeedModifiers(uid);
        }

        var chargingQuery = EntityQueryEnumerator<XenoActiveChargingSpitComponent>();
        while (chargingQuery.MoveNext(out var uid, out var charging))
        {
            if (charging.ExpiresAt > time)
                continue;

            RemCompDeferred<XenoActiveChargingSpitComponent>(uid);
            _popup.PopupClient(Loc.GetString("cm-xeno-charge-spit-expire"), uid, uid, PopupType.SmallCaution);
        }

        var acidedQuery = EntityQueryEnumerator<UserAcidedComponent>();
        while (acidedQuery.MoveNext(out var uid, out var acided))
        {
            if (time >= acided.ExpiresAt)
            {
                RemCompDeferred<UserAcidedComponent>(uid);
                continue;
            }

            if (time < acided.NextDamageAt)
                continue;

            acided.NextDamageAt = time + acided.DamageEvery;
            _damageable.TryChangeDamage(uid, acided.Damage, armorPiercing: acided.ArmorPiercing);
        }
    }
}
