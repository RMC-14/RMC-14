using Content.Shared._RMC14.Chemistry;
using Content.Shared._RMC14.NightVision;
using Content.Shared._RMC14.Stealth;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids.Devour;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Explosion.Components.OnTrigger;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Armor.ThermalCloak;

/// <summary>
/// Handles Thermal Cloak's cloaking ability
/// </summary>
public sealed class ThermalCloakSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalCloakComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<ThermalCloakComponent, ThermalCloakTurnInvisibleActionEvent>(OnCloakAction);
        SubscribeLocalEvent<ThermalCloakComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ThermalCloakComponent, GotUnequippedEvent>(OnUnequipped);

        SubscribeLocalEvent<EntityActiveInvisibleComponent, VaporHitEvent>(OnVaporHit);
        SubscribeLocalEvent<EntityActiveInvisibleComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<EntityActiveInvisibleComponent, XenoDevouredEvent>(OnDevour);

        SubscribeLocalEvent<GunComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<ExplodeOnTriggerComponent, UseInHandEvent>(OnTimerUse);
        SubscribeLocalEvent<UncloakOnHitComponent, ProjectileHitEvent>(OnAcidProjectile);
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
        if (_timing.ApplyingState)
            return;

        if (!_inventory.InSlotWithFlags((ent, null, null), SlotFlags.BACK))
            return;

        var comp = EnsureComp<EntityTurnInvisibleComponent>(args.Equipee);
        comp.RestrictWeapons = ent.Comp.RestrictWeapons;
        comp.UncloakWeaponLock = ent.Comp.UncloakWeaponLock;
        Dirty(args.Equipee, comp);
    }

    private void OnUnequipped(Entity<ThermalCloakComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (_inventory.InSlotWithFlags((ent, null, null), SlotFlags.BACK))
            return;

        SetInvisibility(ent, args.Equipee, false, false);
        RemCompDeferred<EntityTurnInvisibleComponent>(args.Equipee);
    }

    public void SetInvisibility(Entity<ThermalCloakComponent> ent, EntityUid user, bool enabling, bool forced)
    {
        if (!TryComp<EntityTurnInvisibleComponent>(user, out var turnInvisible))
            return;

        if (enabling && !HasComp<EntityActiveInvisibleComponent>(user))
        {
            var activeInvisibility = EnsureComp<EntityActiveInvisibleComponent>(user);
            activeInvisibility.Opacity = ent.Comp.Opacity;
            Dirty(user, activeInvisibility);

            ent.Comp.Enabled = true;
            turnInvisible.Enabled = true;
            if (TryComp<InstantActionComponent>(ent.Comp.Action, out var action))
            {
                action.Cooldown = (_timing.CurTime, _timing.CurTime + ent.Comp.Cooldown);
                action.UseDelay = ent.Comp.Cooldown;
                Dirty(ent.Comp.Action.Value, action);
            }

            if (ent.Comp.HideNightVision)
                RemCompDeferred<RMCNightVisionVisibleComponent>(user);

            if (ent.Comp.BlockFriendlyFire)
                EnsureComp<EntityIFFComponent>(user);

            turnInvisible.UncloakTime = _timing.CurTime; // Just in case

            ToggleLayers(user, ent.Comp.CloakedHideLayers, false);

            if (_net.IsServer)
                SpawnAttachedTo(ent.Comp.CloakEffect, user.ToCoordinates());

            var popupOthers = Loc.GetString("rmc-cloak-activate-others", ("user", user));
            _popup.PopupPredicted(Loc.GetString("rmc-cloak-activate-self"), popupOthers, user, user, PopupType.Medium);

            if (_net.IsServer)
                _audio.PlayPvs(ent.Comp.CloakSound, user);

            return;
        }

        if (!enabling && TryComp<EntityActiveInvisibleComponent>(user, out var invisible))
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

            ToggleLayers(user, ent.Comp.CloakedHideLayers, true);

            if (_net.IsServer)
                SpawnAttachedTo(ent.Comp.UncloakEffect, user.ToCoordinates());

            if (ent.Comp.HideNightVision)
                EnsureComp<RMCNightVisionVisibleComponent>(user);

            if (ent.Comp.BlockFriendlyFire)
                RemCompDeferred<EntityIFFComponent>(user);

            RemCompDeferred<EntityActiveInvisibleComponent>(user);

            if (_net.IsServer)
                _audio.PlayPvs(ent.Comp.UncloakSound, user);
        }
    }

    public void TrySetInvisibility(EntityUid uid, bool enabling, bool forced, ThermalCloakComponent? component = null)
    {
        var cloak = FindWornCloak(uid);
        if (cloak.HasValue)
            SetInvisibility(cloak.Value, uid, false, true);
    }

    private void OnAttemptShoot(Entity<GunComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled || !TryComp<EntityTurnInvisibleComponent>(args.User, out var comp))
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
        if (args.Handled || !TryComp<EntityTurnInvisibleComponent>(args.User, out var comp))
            return;

        if (comp.RestrictWeapons && comp.Enabled || comp.UncloakTime + comp.UncloakWeaponLock > _timing.CurTime)
        {
            args.Handled = true;

            var popup = Loc.GetString("rmc-cloak-attempt-prime");
            _popup.PopupClient(popup, args.User, args.User, PopupType.SmallCaution);
        }
    }

    private void OnAcidProjectile(Entity<UncloakOnHitComponent> ent, ref ProjectileHitEvent args)
    {
        TrySetInvisibility(args.Target, false, true);
    }

    private void OnVaporHit(Entity<EntityActiveInvisibleComponent> ent, ref VaporHitEvent args)
    {
        TrySetInvisibility(ent.Owner, false, true);
    }

    private void OnMobStateChanged(Entity<EntityActiveInvisibleComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        TrySetInvisibility(ent.Owner, false, true);
    }

    private void OnDevour(Entity<EntityActiveInvisibleComponent> ent, ref XenoDevouredEvent args)
    {
        TrySetInvisibility(ent.Owner, false, true);
    }

    private Entity<ThermalCloakComponent>? FindWornCloak(EntityUid player)
    {
        var slots = _inventory.GetSlotEnumerator(player, SlotFlags.BACK);
        while (slots.MoveNext(out var slot))
        {
            if (TryComp<ThermalCloakComponent>(slot.ContainedEntity, out var comp))
                return (slot.ContainedEntity.Value, comp);
        }

        return null;
    }

    private void ToggleLayers(EntityUid equipee, HashSet<HumanoidVisualLayers> layers, bool showLayers)
    {
        foreach (HumanoidVisualLayers layer in layers)
        {
            _humanoidSystem.SetLayerVisibility(equipee, layer, showLayers);
        }
    }
}
