using Content.Shared._RMC14.Chemistry;
using Content.Shared._RMC14.Marines.Invisibility;
using Content.Shared._RMC14.Projectiles;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared._RMC14.Xenonids.Projectile.Spit.Charge;
using Content.Shared.Actions;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Whitelist;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Explosion.Components.OnTrigger;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Armor.ThermalCloak;

/// <summary>
/// Handles Thermal Cloak's cloaking ability
/// </summary>
public sealed class ThermalCloakSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalCloakComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<ThermalCloakComponent, ThermalCloakTurnInvisibleActionEvent>(OnCloakAction);
        SubscribeLocalEvent<ThermalCloakComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ThermalCloakComponent, GotUnequippedEvent>(OnUnequipped);

        SubscribeLocalEvent<MarineActiveInvisibleComponent, VaporHitEvent>(OnVaporHit);

        SubscribeLocalEvent<GunComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<ExplodeOnTriggerComponent, UseInHandEvent>(OnTimerUse);
        SubscribeLocalEvent<UncloakOnHitComponent, ProjectileHitEvent>(OnAcidProjectile);
        SubscribeLocalEvent<UncloakOnHitComponent, StartCollideEvent>(OnAcidSpray);
    }

    private void OnGetItemActions(Entity<ThermalCloakComponent> ent, ref GetItemActionsEvent args)
    {
        var comp = ent.Comp;

        if (args.InHands || !_inventory.InSlotWithFlags((ent, null, null), SlotFlags.BACK))
            return;

        args.AddAction(ref comp.Action, comp.ActionId);
        Dirty(ent);
    }

    private void OnCloakAction(Entity<ThermalCloakComponent> ent, ref ThermalCloakTurnInvisibleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!_whitelist.IsValid(ent.Comp.Whitelist, args.Performer))
        {
            var popup = Loc.GetString("cm-gun-unskilled", ("gun", ent.Owner));
            _popup.PopupClient(popup, args.Performer, args.Performer, PopupType.SmallCaution);
            return;
        }

        SetInvisibility(ent, args.Performer, !ent.Comp.Enabled, false);
    }

    private void OnEquipped(Entity<ThermalCloakComponent> ent, ref GotEquippedEvent args)
    {
        if (!_inventory.InSlotWithFlags((ent, null, null), SlotFlags.BACK))
            return;

        var comp = EnsureComp<MarineTurnInvisibleComponent>(args.Equipee);
        comp.Opacity = ent.Comp.Opacity;
        comp.RestrictWeapons = ent.Comp.RestrictWeapons;
        comp.UncloakWeaponLock = ent.Comp.UncloakWeaponLock;
    }

    private void OnUnequipped(Entity<ThermalCloakComponent> ent, ref GotUnequippedEvent args)
    {
        if (_inventory.InSlotWithFlags((ent, null, null), SlotFlags.BACK))
            return;

        SetInvisibility(ent, args.Equipee, false, false);
        RemCompDeferred<MarineTurnInvisibleComponent>(args.Equipee);
    }

    public void SetInvisibility(Entity<ThermalCloakComponent> ent, EntityUid user, bool enabling, bool forced)
    {
        if (!TryComp<MarineTurnInvisibleComponent>(user, out var turnInvisible))
            return;

        if (enabling && !HasComp<MarineActiveInvisibleComponent>(user))
        {
            var activeInvisibility = EnsureComp<MarineActiveInvisibleComponent>(user);
            activeInvisibility.Opacity = ent.Comp.Opacity;

            ent.Comp.Enabled = true;
            turnInvisible.Enabled = true;
            if (TryComp<InstantActionComponent>(ent.Comp.Action, out var action))
            {
                action.Cooldown = (_timing.CurTime, _timing.CurTime + ent.Comp.Cooldown);
                action.UseDelay = ent.Comp.Cooldown;
                Dirty(ent.Comp.Action.Value, action);
            }

            turnInvisible.UncloakTime = _timing.CurTime; // Just in case

            var popupOthers = Loc.GetString("rmc-cloak-activate-others", ("user", user));
            _popup.PopupPredicted(Loc.GetString("rmc-cloak-activate-self"), popupOthers, user, user, PopupType.Medium);
            _audio.PlayPvs(ent.Comp.CloakSound, user);
            return;
        }

        if (!enabling && TryComp<MarineActiveInvisibleComponent>(user, out var invisible))
        {
            invisible.Opacity = 1;
            Dirty(user, invisible);
            ent.Comp.Enabled = false;
            turnInvisible.Enabled = false;

            if (forced)
            {
                if (TryComp<InstantActionComponent>(ent.Comp.Action, out var action))
                {
                    action.Cooldown = (_timing.CurTime, _timing.CurTime + ent.Comp.ForcedCooldown);
                    action.UseDelay = ent.Comp.ForcedCooldown;
                    Dirty(ent.Comp.Action.Value, action);
                }

                turnInvisible.UncloakTime = _timing.CurTime;

                var forcedPopupOthers = Loc.GetString("rmc-cloak-forced-deactivate-others", ("user", user));
                _popup.PopupPredicted(Loc.GetString("rmc-cloak-forced-deactivate-self"), forcedPopupOthers, user, user, PopupType.Medium);
            }
            else
            {
                if (TryComp<InstantActionComponent>(ent.Comp.Action, out var action))
                {
                    action.Cooldown = (_timing.CurTime, _timing.CurTime + ent.Comp.Cooldown);
                    action.UseDelay = ent.Comp.Cooldown;
                    Dirty(ent.Comp.Action.Value, action);
                }

                turnInvisible.UncloakTime = _timing.CurTime;
                var popupOthers = Loc.GetString("rmc-cloak-deactivate-others", ("user", user));
                _popup.PopupPredicted(Loc.GetString("rmc-cloak-deactivate-self"), popupOthers, user, user, PopupType.Medium);
            }

            RemCompDeferred<MarineActiveInvisibleComponent>(user);
            _audio.PlayPvs(ent.Comp.UncloakSound, user);
        }
    }

    private void OnAttemptShoot(Entity<GunComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled || !TryComp<MarineTurnInvisibleComponent>(args.User, out var comp))
            return;

        if (comp.RestrictWeapons && comp.Enabled || comp.UncloakTime + comp.UncloakWeaponLock > _timing.CurTime)
        {
            args.Cancelled = true;

            var popup = Loc.GetString("rmc-cloak-attempt-shoot");
            _popup.PopupClient(popup, args.User, args.User, PopupType.SmallCaution);
        }
    }

    private void OnTimerUse(Entity<ExplodeOnTriggerComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !TryComp<MarineTurnInvisibleComponent>(args.User, out var comp))
            return;

        if (comp.RestrictWeapons && comp.Enabled || comp.UncloakTime + comp.UncloakWeaponLock > _timing.CurTime)
        {
            args.Handled = true;

            var popup = Loc.GetString("rmc-cloak-attempt-prime");
            _popup.PopupClient(popup, args.User, args.User, PopupType.SmallCaution);
        }
    }

    private void OnVaporHit(Entity<MarineActiveInvisibleComponent> ent, ref VaporHitEvent args)
    {
        var slots = _inventory.GetSlotEnumerator(ent.Owner, SlotFlags.BACK);
        while (slots.MoveNext(out var slot))
        {
            if (TryComp<ThermalCloakComponent>(slot.ContainedEntity, out var comp))
                SetInvisibility((slot.ContainedEntity.Value, comp), ent.Owner, false, true);
        }
    }

    private void OnAcidProjectile(Entity<UncloakOnHitComponent> ent, ref ProjectileHitEvent args)
    {
        var slots = _inventory.GetSlotEnumerator(args.Target, SlotFlags.BACK);
        while (slots.MoveNext(out var slot))
        {
            if (TryComp<ThermalCloakComponent>(slot.ContainedEntity, out var comp))
                SetInvisibility((slot.ContainedEntity.Value, comp), args.Target, false, true);
        }
    }

    private void OnAcidSpray(Entity<UncloakOnHitComponent> ent, ref StartCollideEvent args)
    {
        var slots = _inventory.GetSlotEnumerator(args.OtherEntity, SlotFlags.BACK);
        while (slots.MoveNext(out var slot))
        {
            if (TryComp<ThermalCloakComponent>(slot.ContainedEntity, out var comp))
                SetInvisibility((slot.ContainedEntity.Value, comp), args.OtherEntity, false, true);
        }
    }
}
