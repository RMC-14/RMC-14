using System;
using System.Collections.Generic;
using Content.Shared._RMC14.Weapons.Ranged.Ammo.BulletBox;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCVehicleAmmoLoaderSystem : EntitySystem
{
    [Dependency] private readonly BulletBoxSystem _bulletBox = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly RMCVehicleHardpointAmmoSystem _hardpointAmmo = default!;
    [Dependency] private readonly RMCVehicleSystem _vehicleSystem = default!;

    private readonly Dictionary<EntityUid, Dictionary<EntityUid, EntityUid>> _activeAmmoBoxes = new();
    private readonly Dictionary<EntityUid, HashSet<EntityUid>> _openLoadersByUser = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCVehicleAmmoLoaderComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCVehicleAmmoLoaderComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<RMCVehicleAmmoLoaderComponent, VehicleAmmoLoaderDoAfterEvent>(OnLoadDoAfter);
        SubscribeLocalEvent<RMCVehicleAmmoLoaderComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<RMCVehicleAmmoLoaderComponent, BoundUIClosedEvent>(OnUiClosed);
        SubscribeLocalEvent<RMCVehicleAmmoLoaderComponent, RMCVehicleAmmoLoaderSelectMessage>(OnUiSelect);
        SubscribeLocalEvent<HandsComponent, DidEquipHandEvent>(OnHandItemChanged);
        SubscribeLocalEvent<HandsComponent, DidUnequipHandEvent>(OnHandItemChanged);
        SubscribeLocalEvent<BulletBoxComponent, HandSelectedEvent>(OnBulletBoxHandSelected);
        SubscribeLocalEvent<BulletBoxComponent, HandDeselectedEvent>(OnBulletBoxHandDeselected);
    }

    private void OnInteractUsing(Entity<RMCVehicleAmmoLoaderComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || _net.IsClient)
            return;

        if (!TryComp(args.Used, out BulletBoxComponent? _))
            return;

        TrySetActiveAmmoBox(ent.Owner, args.User, args.Used);

        if (!TryOpenUi(ent, args.User))
            return;

        args.Handled = true;
    }

    private void OnInteractHand(Entity<RMCVehicleAmmoLoaderComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || _net.IsClient)
            return;

        if (!TryOpenUi(ent, args.User))
            return;

        args.Handled = true;
    }

    private bool TryOpenUi(Entity<RMCVehicleAmmoLoaderComponent> ent, EntityUid user)
    {
        if (!TryComp(ent.Owner, out UserInterfaceComponent? uiComp) ||
            !_ui.HasUi(ent.Owner, RMCVehicleAmmoLoaderUiKey.Key, uiComp))
        {
            return false;
        }

        if (!_vehicleSystem.TryGetVehicleFromInterior(ent.Owner, out var vehicleUid) || vehicleUid == null)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-no-vehicle"), ent, user);
            return false;
        }

        if (!TryComp(vehicleUid.Value, out RMCHardpointSlotsComponent? hardpoints) ||
            !TryComp(vehicleUid.Value, out ItemSlotsComponent? itemSlots))
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-no-hardpoint"), ent, user);
            return false;
        }

        if (!CanOpenUi(ent.Owner, user))
            return false;

        TrySetActiveAmmoBoxFromHeld(ent, user);

        _ui.OpenUi(ent.Owner, RMCVehicleAmmoLoaderUiKey.Key, user);
        UpdateUi(ent.Owner, user);
        return true;
    }

    private void OnLoadDoAfter(Entity<RMCVehicleAmmoLoaderComponent> ent, ref VehicleAmmoLoaderDoAfterEvent args)
    {
        if (_net.IsClient || args.Cancelled || args.Handled)
            return;

        if (string.IsNullOrWhiteSpace(args.SlotId))
            return;

        if (args.Action == RMCVehicleAmmoLoaderSlotAction.Unload)
        {
            if (!TryGetUnloadableAmmoProvider(ent, args.User, args.SlotId, out _, out var directAmmoUid, out var directAmmo, out var directHardpointAmmo, out var refill))
                return;

            DoDirectUnloadAmmoSlot(ent, args.User, directAmmoUid, directAmmo, directHardpointAmmo, refill, args.AmmoSlot);
            UpdateUi(ent.Owner, args.User);
            args.Handled = true;
            return;
        }

        if (args.Used is not { } used || !TryComp(used, out BulletBoxComponent? box))
            return;

        if (!TryGetActiveAmmoBox(ent.Owner, args.User, out var activeBox) || activeBox != used)
            return;

        if (!TryGetLoadableAmmoProvider(ent, args.User, box, args.SlotId, out _, out var ammoUid, out var ammo, out var hardpointAmmo))
            return;

        if (args.Action == RMCVehicleAmmoLoaderSlotAction.Load)
            DoLoadAmmoSlot(ent, args.User, (used, box), ammoUid, ammo, hardpointAmmo, args.AmmoSlot);

        UpdateUi(ent.Owner, args.User);
        args.Handled = true;
    }

    private void OnUiOpened(Entity<RMCVehicleAmmoLoaderComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!Equals(args.UiKey, RMCVehicleAmmoLoaderUiKey.Key))
            return;

        TrackOpenLoader(ent.Owner, args.Actor);

        if (!_net.IsClient)
            UpdateUi(ent.Owner, args.Actor);
    }

    private void OnUiClosed(Entity<RMCVehicleAmmoLoaderComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!Equals(args.UiKey, RMCVehicleAmmoLoaderUiKey.Key))
            return;

        UntrackOpenLoader(ent.Owner, args.Actor);
        ClearActiveAmmoBox(ent.Owner, args.Actor);
    }

    private void OnHandItemChanged(Entity<HandsComponent> ent, ref DidEquipHandEvent args)
    {
        UpdateOpenLoaderUis(args.User);
    }

    private void OnHandItemChanged(Entity<HandsComponent> ent, ref DidUnequipHandEvent args)
    {
        UpdateOpenLoaderUis(args.User);
    }

    private void OnBulletBoxHandSelected(Entity<BulletBoxComponent> ent, ref HandSelectedEvent args)
    {
        UpdateOpenLoaderUis(args.User);
    }

    private void OnBulletBoxHandDeselected(Entity<BulletBoxComponent> ent, ref HandDeselectedEvent args)
    {
        UpdateOpenLoaderUis(args.User);
    }

    private void OnUiSelect(Entity<RMCVehicleAmmoLoaderComponent> ent, ref RMCVehicleAmmoLoaderSelectMessage args)
    {
        if (!Equals(args.UiKey, RMCVehicleAmmoLoaderUiKey.Key))
            return;

        if (args.Actor == default || !Exists(args.Actor))
            return;

        if (args.Action == RMCVehicleAmmoLoaderSlotAction.Unload)
        {
            if (!TryGetUnloadableAmmoProvider(ent, args.Actor, args.SlotId, out _, out var directAmmoUid, out var directAmmo, out var directHardpointAmmo, out _))
                return;

            if (GetDirectUnloadAmount(directAmmo, directHardpointAmmo, args.AmmoSlot) <= 0)
            {
                _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-empty", ("box", directAmmoUid)), ent, args.Actor);
                return;
            }

            var directDoAfter = new DoAfterArgs(
                EntityManager,
                args.Actor,
                ent.Comp.LoadDelay,
                new VehicleAmmoLoaderDoAfterEvent(args.SlotId, args.AmmoSlot, args.Action),
                ent.Owner,
                ent.Owner)
            {
                BreakOnMove = true,
                CancelDuplicate = false,
                DistanceThreshold = ent.Comp.InteractionRange,
            };

            _doAfter.TryStartDoAfter(directDoAfter);
            return;
        }

        if (!_hands.TryGetActiveItem(args.Actor, out var activeItem) ||
            activeItem is not { } activeBox ||
            !TryComp(activeBox, out BulletBoxComponent? box))
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-hold-ammo"), ent, args.Actor);
            return;
        }

        TrySetActiveAmmoBox(ent.Owner, args.Actor, activeBox);

        if (!TryGetLoadableAmmoProvider(ent, args.Actor, box, args.SlotId, out _, out var ammoUid, out var ammo, out var hardpointAmmo))
            return;

        if (args.Action == RMCVehicleAmmoLoaderSlotAction.Load &&
            GetLoadAmount(box, ammo, hardpointAmmo, args.AmmoSlot) <= 0)
        {
            var popup = box.Amount <= 0
                ? Loc.GetString("rmc-vehicle-ammo-loader-empty", ("box", box.Owner))
                : Loc.GetString("rmc-vehicle-ammo-loader-full", ("target", ammoUid));
            _popup.PopupClient(popup, ent, args.Actor);
            return;
        }

        var doAfter = new DoAfterArgs(
            EntityManager,
            args.Actor,
            ent.Comp.LoadDelay,
            new VehicleAmmoLoaderDoAfterEvent(args.SlotId, args.AmmoSlot, args.Action),
            ent.Owner,
            ent.Owner,
            activeBox)
        {
            BreakOnMove = true,
            BreakOnDropItem = true,
            CancelDuplicate = false,
            DistanceThreshold = ent.Comp.InteractionRange,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private bool CanOpenUi(EntityUid loader, EntityUid user)
    {
        foreach (var actor in _ui.GetActors(loader, RMCVehicleAmmoLoaderUiKey.Key))
        {
            if (actor == user)
                return true;

            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-in-use"), loader, user);
            return false;
        }

        return true;
    }

    private bool TrySetActiveAmmoBox(EntityUid loader, EntityUid user, EntityUid boxUid)
    {
        if (!_activeAmmoBoxes.TryGetValue(loader, out var userBoxes))
        {
            userBoxes = new Dictionary<EntityUid, EntityUid>();
            _activeAmmoBoxes[loader] = userBoxes;
        }

        userBoxes[user] = boxUid;
        return true;
    }

    private void TrySetActiveAmmoBoxFromHeld(Entity<RMCVehicleAmmoLoaderComponent> loader, EntityUid user)
    {
        if (_hands.TryGetActiveItem(user, out var activeItem) &&
            activeItem is { } active &&
            TryComp(active, out BulletBoxComponent? _))
        {
            TrySetActiveAmmoBox(loader.Owner, user, active);
            return;
        }

        ClearActiveAmmoBox(loader.Owner, user);
    }

    private bool TryGetActiveAmmoBox(EntityUid loader, EntityUid user, out EntityUid boxUid)
    {
        boxUid = default;
        return _activeAmmoBoxes.TryGetValue(loader, out var userBoxes) &&
               userBoxes.TryGetValue(user, out boxUid);
    }

    private void ClearActiveAmmoBox(EntityUid loader, EntityUid user)
    {
        if (!_activeAmmoBoxes.TryGetValue(loader, out var userBoxes))
            return;

        userBoxes.Remove(user);
        if (userBoxes.Count == 0)
            _activeAmmoBoxes.Remove(loader);
    }

    private void TrackOpenLoader(EntityUid loader, EntityUid user)
    {
        if (!_openLoadersByUser.TryGetValue(user, out var loaders))
        {
            loaders = new HashSet<EntityUid>();
            _openLoadersByUser[user] = loaders;
        }

        loaders.Add(loader);
    }

    private void UntrackOpenLoader(EntityUid loader, EntityUid user)
    {
        if (!_openLoadersByUser.TryGetValue(user, out var loaders))
            return;

        loaders.Remove(loader);
        if (loaders.Count == 0)
            _openLoadersByUser.Remove(user);
    }

    private void UpdateOpenLoaderUis(EntityUid user)
    {
        if (_net.IsClient || !_openLoadersByUser.TryGetValue(user, out var loaders))
            return;

        var toRemove = new List<EntityUid>();
        foreach (var loader in loaders)
        {
            if (!Exists(loader))
            {
                toRemove.Add(loader);
                continue;
            }

            UpdateUi(loader, user);
        }

        foreach (var loader in toRemove)
            loaders.Remove(loader);

        if (loaders.Count == 0)
            _openLoadersByUser.Remove(user);
    }

    private bool TryGetLoadableAmmoProvider(
        Entity<RMCVehicleAmmoLoaderComponent> loader,
        EntityUid user,
        BulletBoxComponent box,
        string? slotId,
        out EntityUid vehicle,
        out EntityUid ammoUid,
        out BallisticAmmoProviderComponent ammo,
        out RMCVehicleHardpointAmmoComponent hardpointAmmo)
    {
        ammoUid = default;
        ammo = default!;
        hardpointAmmo = default!;
        vehicle = default;

        if (!_vehicleSystem.TryGetVehicleFromInterior(loader.Owner, out var vehicleUid) || vehicleUid == null)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-no-vehicle"), loader, user);
            return false;
        }

        vehicle = vehicleUid.Value;

        if (loader.Comp.BulletType != null && loader.Comp.BulletType != box.BulletType)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-wrong-ammo"), loader, user);
            return false;
        }

        if (!TryComp(vehicle, out RMCHardpointSlotsComponent? hardpoints) ||
            !TryComp(vehicle, out ItemSlotsComponent? itemSlots))
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-no-hardpoint"), loader, user);
            return false;
        }

        if (!TryFindAmmoProvider(vehicle, hardpoints, itemSlots, loader.Comp, box, slotId, out ammoUid, out ammo, out hardpointAmmo, out var result))
        {
            var popup = result == AmmoLoaderLookupResult.WrongAmmo
                ? Loc.GetString("rmc-vehicle-ammo-loader-wrong-ammo")
                : Loc.GetString("rmc-vehicle-ammo-loader-no-hardpoint");
            _popup.PopupClient(popup, loader, user);
            return false;
        }

        return true;
    }

    private bool TryGetUnloadableAmmoProvider(
        Entity<RMCVehicleAmmoLoaderComponent> loader,
        EntityUid user,
        string? slotId,
        out EntityUid vehicle,
        out EntityUid ammoUid,
        out BallisticAmmoProviderComponent ammo,
        out RMCVehicleHardpointAmmoComponent hardpointAmmo,
        out RefillableByBulletBoxComponent refill)
    {
        ammoUid = default;
        ammo = default!;
        hardpointAmmo = default!;
        refill = default!;
        vehicle = default;

        if (!_vehicleSystem.TryGetVehicleFromInterior(loader.Owner, out var vehicleUid) || vehicleUid == null)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-no-vehicle"), loader, user);
            return false;
        }

        vehicle = vehicleUid.Value;

        if (!TryComp(vehicle, out RMCHardpointSlotsComponent? hardpoints) ||
            !TryComp(vehicle, out ItemSlotsComponent? itemSlots))
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-no-hardpoint"), loader, user);
            return false;
        }

        if (!TryFindAmmoProvider(vehicle, hardpoints, itemSlots, loader.Comp, slotId, out ammoUid, out ammo, out hardpointAmmo, out refill))
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-no-hardpoint"), loader, user);
            return false;
        }

        return true;
    }

    private bool TryFindAmmoProvider(
        EntityUid vehicle,
        RMCHardpointSlotsComponent hardpoints,
        ItemSlotsComponent itemSlots,
        RMCVehicleAmmoLoaderComponent loader,
        BulletBoxComponent box,
        string? slotId,
        out EntityUid ammoUid,
        out BallisticAmmoProviderComponent ammo,
        out RMCVehicleHardpointAmmoComponent hardpointAmmo,
        out AmmoLoaderLookupResult result)
    {
        ammoUid = default;
        ammo = default!;
        hardpointAmmo = default!;
        result = AmmoLoaderLookupResult.NotFound;

        if (!string.IsNullOrWhiteSpace(slotId) &&
            RMCVehicleTurretSlotIds.TryParse(slotId, out var parentSlotId, out var childSlotId))
        {
            if (!TryGetTurretSlot(vehicle, parentSlotId, childSlotId, itemSlots, out var turretSlot, out var item))
                return false;

            if (!string.IsNullOrWhiteSpace(loader.HardpointType) &&
                !string.Equals(turretSlot.HardpointType, loader.HardpointType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (TryGetAmmoProviderFromItem(item, box, out ammoUid, out ammo, out hardpointAmmo, out var wrongAmmo))
                return true;

            if (wrongAmmo)
                result = AmmoLoaderLookupResult.WrongAmmo;

            return false;
        }

        foreach (var slot in hardpoints.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            if (!string.IsNullOrWhiteSpace(slotId) &&
                !string.Equals(slot.Id, slotId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(loader.HardpointType) &&
                !string.Equals(slot.HardpointType, loader.HardpointType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!_itemSlots.TryGetSlot(vehicle, slot.Id, out var itemSlot, itemSlots) || !itemSlot.HasItem)
                continue;

            var item = itemSlot.Item!.Value;
            if (TryGetAmmoProviderFromItem(item, box, out ammoUid, out ammo, out hardpointAmmo, out var wrongAmmo))
                return true;

            if (wrongAmmo)
                result = AmmoLoaderLookupResult.WrongAmmo;

            if (!TryComp(item, out RMCHardpointSlotsComponent? turretSlots) ||
                !TryComp(item, out ItemSlotsComponent? turretItemSlots))
            {
                continue;
            }

            foreach (var turretSlot in turretSlots.Slots)
            {
                if (string.IsNullOrWhiteSpace(turretSlot.Id))
                    continue;

                if (!string.IsNullOrWhiteSpace(loader.HardpointType) &&
                    !string.Equals(turretSlot.HardpointType, loader.HardpointType, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!_itemSlots.TryGetSlot(item, turretSlot.Id, out var turretItemSlot, turretItemSlots) ||
                    !turretItemSlot.HasItem)
                {
                    continue;
                }

                var turretItem = turretItemSlot.Item!.Value;
                if (TryGetAmmoProviderFromItem(turretItem, box, out ammoUid, out ammo, out hardpointAmmo, out wrongAmmo))
                    return true;

                if (wrongAmmo)
                    result = AmmoLoaderLookupResult.WrongAmmo;
            }
        }

        return false;
    }

    private bool TryFindAmmoProvider(
        EntityUid vehicle,
        RMCHardpointSlotsComponent hardpoints,
        ItemSlotsComponent itemSlots,
        RMCVehicleAmmoLoaderComponent loader,
        string? slotId,
        out EntityUid ammoUid,
        out BallisticAmmoProviderComponent ammo,
        out RMCVehicleHardpointAmmoComponent hardpointAmmo,
        out RefillableByBulletBoxComponent refill)
    {
        ammoUid = default;
        ammo = default!;
        hardpointAmmo = default!;
        refill = default!;

        if (!string.IsNullOrWhiteSpace(slotId) &&
            RMCVehicleTurretSlotIds.TryParse(slotId, out var parentSlotId, out var childSlotId))
        {
            if (!TryGetTurretSlot(vehicle, parentSlotId, childSlotId, itemSlots, out var turretSlot, out var item))
                return false;

            if (!string.IsNullOrWhiteSpace(loader.HardpointType) &&
                !string.Equals(turretSlot.HardpointType, loader.HardpointType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return TryGetAmmoProviderFromItem(item, out ammoUid, out ammo, out hardpointAmmo, out refill);
        }

        foreach (var slot in hardpoints.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            if (!string.IsNullOrWhiteSpace(slotId) &&
                !string.Equals(slot.Id, slotId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(loader.HardpointType) &&
                !string.Equals(slot.HardpointType, loader.HardpointType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!_itemSlots.TryGetSlot(vehicle, slot.Id, out var itemSlot, itemSlots) || !itemSlot.HasItem)
                continue;

            var item = itemSlot.Item!.Value;
            if (TryGetAmmoProviderFromItem(item, out ammoUid, out ammo, out hardpointAmmo, out refill))
                return true;

            if (!TryComp(item, out RMCHardpointSlotsComponent? turretSlots) ||
                !TryComp(item, out ItemSlotsComponent? turretItemSlots))
            {
                continue;
            }

            foreach (var turretSlot in turretSlots.Slots)
            {
                if (string.IsNullOrWhiteSpace(turretSlot.Id))
                    continue;

                if (!string.IsNullOrWhiteSpace(loader.HardpointType) &&
                    !string.Equals(turretSlot.HardpointType, loader.HardpointType, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!_itemSlots.TryGetSlot(item, turretSlot.Id, out var turretItemSlot, turretItemSlots) ||
                    !turretItemSlot.HasItem)
                {
                    continue;
                }

                var turretItem = turretItemSlot.Item!.Value;
                if (TryGetAmmoProviderFromItem(turretItem, out ammoUid, out ammo, out hardpointAmmo, out refill))
                    return true;
            }
        }

        return false;
    }

    private void UpdateUi(EntityUid loader, EntityUid user)
    {
        if (_net.IsClient)
            return;

        if (!TryComp(loader, out RMCVehicleAmmoLoaderComponent? loaderComp))
            return;

        if (!_vehicleSystem.TryGetVehicleFromInterior(loader, out var vehicleUid) || vehicleUid == null)
            return;

        if (!TryComp(vehicleUid.Value, out RMCHardpointSlotsComponent? hardpoints) ||
            !TryComp(vehicleUid.Value, out ItemSlotsComponent? itemSlots))
        {
            return;
        }

        TrySetActiveAmmoBoxFromHeld((loader, loaderComp), user);

        BulletBoxComponent? heldBox = null;
        if (TryGetActiveAmmoBox(loader, user, out var boxUid))
            TryComp(boxUid, out heldBox);

        var entries = new List<RMCVehicleAmmoLoaderUiEntry>(hardpoints.Slots.Count);

        foreach (var slot in hardpoints.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            if (!string.IsNullOrWhiteSpace(loaderComp.HardpointType) &&
                !string.Equals(slot.HardpointType, loaderComp.HardpointType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!_itemSlots.TryGetSlot(vehicleUid.Value, slot.Id, out var itemSlot, itemSlots) || !itemSlot.HasItem)
                continue;

            var item = itemSlot.Item!.Value;
            AppendTurretAmmoEntries(entries, item, slot.Id, loaderComp, heldBox);

            if (!TryGetAmmoProviderFromItem(item, out _, out var ammoProvider, out var hardpointAmmo, out var refill))
                continue;

            if (loaderComp.BulletType != null && loaderComp.BulletType != refill.BulletType)
                continue;

            var magazineSize = _hardpointAmmo.GetMagazineSize(hardpointAmmo, ammoProvider);
            var ammoSlots = GetAmmoSlotUiEntries(heldBox, refill.BulletType, ammoProvider, hardpointAmmo, magazineSize);
            var canLoad = false;
            var canUnload = false;
            foreach (var ammoSlot in ammoSlots)
            {
                canLoad |= ammoSlot.CanLoad;
                canUnload |= ammoSlot.CanUnload;
            }

            var name = Name(item);
            entries.Add(new RMCVehicleAmmoLoaderUiEntry(
                slot.Id,
                slot.HardpointType,
                name,
                GetNetEntity(item),
                refill.BulletType,
                magazineSize,
                ammoSlots,
                canLoad,
                canUnload));
        }

        _ui.SetUiState(
            loader,
            RMCVehicleAmmoLoaderUiKey.Key,
            new RMCVehicleAmmoLoaderUiState(
                entries,
                heldBox?.Amount ?? 0,
                heldBox?.Max ?? 0,
                heldBox?.BulletType));
    }

    private void AppendTurretAmmoEntries(
        List<RMCVehicleAmmoLoaderUiEntry> entries,
        EntityUid turretUid,
        string parentSlotId,
        RMCVehicleAmmoLoaderComponent loaderComp,
        BulletBoxComponent? heldBox)
    {
        if (!TryComp(turretUid, out RMCHardpointSlotsComponent? turretSlots) ||
            !TryComp(turretUid, out ItemSlotsComponent? turretItemSlots))
        {
            return;
        }

        foreach (var turretSlot in turretSlots.Slots)
        {
            if (string.IsNullOrWhiteSpace(turretSlot.Id))
                continue;

            if (!string.IsNullOrWhiteSpace(loaderComp.HardpointType) &&
                !string.Equals(turretSlot.HardpointType, loaderComp.HardpointType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!_itemSlots.TryGetSlot(turretUid, turretSlot.Id, out var turretItemSlot, turretItemSlots) ||
                !turretItemSlot.HasItem)
            {
                continue;
            }

            var item = turretItemSlot.Item!.Value;
            if (!TryGetAmmoProviderFromItem(item, out _, out var ammoProvider, out var hardpointAmmo, out var refill))
                continue;

            if (loaderComp.BulletType != null && loaderComp.BulletType != refill.BulletType)
                continue;

            var magazineSize = _hardpointAmmo.GetMagazineSize(hardpointAmmo, ammoProvider);
            var ammoSlots = GetAmmoSlotUiEntries(heldBox, refill.BulletType, ammoProvider, hardpointAmmo, magazineSize);
            var canLoad = false;
            var canUnload = false;
            foreach (var ammoSlot in ammoSlots)
            {
                canLoad |= ammoSlot.CanLoad;
                canUnload |= ammoSlot.CanUnload;
            }

            entries.Add(new RMCVehicleAmmoLoaderUiEntry(
                RMCVehicleTurretSlotIds.Compose(parentSlotId, turretSlot.Id),
                turretSlot.HardpointType,
                Name(item),
                GetNetEntity(item),
                refill.BulletType,
                magazineSize,
                ammoSlots,
                canLoad,
                canUnload));
        }
    }

    private List<RMCVehicleAmmoLoaderUiAmmoSlot> GetAmmoSlotUiEntries(
        BulletBoxComponent? heldBox,
        EntProtoId? ammoPrototype,
        BallisticAmmoProviderComponent ammo,
        RMCVehicleHardpointAmmoComponent hardpointAmmo,
        int magazineSize)
    {
        var entries = new List<RMCVehicleAmmoLoaderUiAmmoSlot>(Math.Max(1, hardpointAmmo.MaxStoredMagazines));
        var activeRounds = Math.Clamp(ammo.Count, 0, magazineSize);
        entries.Add(new RMCVehicleAmmoLoaderUiAmmoSlot(
            0,
            Loc.GetString("rmc-vehicle-ammo-loader-ui-ready-slot"),
            activeRounds,
            magazineSize,
            CanLoadSlot(heldBox, ammoPrototype, ammo, hardpointAmmo, 0),
            CanUnloadSlot(ammo, hardpointAmmo, 0),
            true));

        var reserveSlots = _hardpointAmmo.GetStoredRoundSlots(hardpointAmmo, magazineSize);
        for (var i = 0; i < reserveSlots.Count; i++)
        {
            var slotIndex = i + 1;
            entries.Add(new RMCVehicleAmmoLoaderUiAmmoSlot(
                slotIndex,
                (slotIndex + 1).ToString(),
                reserveSlots[i],
                magazineSize,
                CanLoadSlot(heldBox, ammoPrototype, ammo, hardpointAmmo, slotIndex),
                CanUnloadSlot(ammo, hardpointAmmo, slotIndex),
                false));
        }

        return entries;
    }

    private bool CanLoadSlot(
        BulletBoxComponent? heldBox,
        EntProtoId? ammoPrototype,
        BallisticAmmoProviderComponent ammo,
        RMCVehicleHardpointAmmoComponent hardpointAmmo,
        int ammoSlot)
    {
        return heldBox != null &&
               heldBox.Amount > 0 &&
               ammoPrototype != null &&
               heldBox.BulletType == ammoPrototype &&
               HasLoadSpace(ammo, hardpointAmmo, ammoSlot);
    }

    private bool CanUnloadSlot(
        BallisticAmmoProviderComponent ammo,
        RMCVehicleHardpointAmmoComponent hardpointAmmo,
        int ammoSlot)
    {
        if (!HasUnloadRounds(ammo, hardpointAmmo, ammoSlot))
            return false;

        return true;
    }

    private bool HasLoadSpace(
        BallisticAmmoProviderComponent ammo,
        RMCVehicleHardpointAmmoComponent hardpointAmmo,
        int ammoSlot)
    {
        if (ammoSlot < 0)
            return false;

        var magazineSize = _hardpointAmmo.GetMagazineSize(hardpointAmmo, ammo);
        if (ammoSlot == 0)
            return Math.Clamp(ammo.Count, 0, magazineSize) < magazineSize;

        var reserveSlot = ammoSlot - 1;
        if (reserveSlot >= _hardpointAmmo.GetMaxStoredRoundSlots(hardpointAmmo))
            return false;

        return _hardpointAmmo.GetStoredSlotRounds(hardpointAmmo, reserveSlot, magazineSize) < magazineSize;
    }

    private bool HasUnloadRounds(
        BallisticAmmoProviderComponent ammo,
        RMCVehicleHardpointAmmoComponent hardpointAmmo,
        int ammoSlot)
    {
        if (ammoSlot < 0)
            return false;

        var magazineSize = _hardpointAmmo.GetMagazineSize(hardpointAmmo, ammo);
        if (ammoSlot == 0)
            return Math.Min(Math.Clamp(ammo.Count, 0, magazineSize), ammo.UnspawnedCount) > 0;

        var reserveSlot = ammoSlot - 1;
        if (reserveSlot >= _hardpointAmmo.GetMaxStoredRoundSlots(hardpointAmmo))
            return false;

        return _hardpointAmmo.GetStoredSlotRounds(hardpointAmmo, reserveSlot, magazineSize) > 0;
    }

    private int GetLoadAmount(
        BulletBoxComponent box,
        BallisticAmmoProviderComponent ammo,
        RMCVehicleHardpointAmmoComponent hardpointAmmo,
        int ammoSlot)
    {
        if (ammoSlot < 0)
            return 0;

        if (box.Amount <= 0)
            return 0;

        var magazineSize = _hardpointAmmo.GetMagazineSize(hardpointAmmo, ammo);
        if (ammoSlot == 0)
        {
            var chambered = Math.Clamp(ammo.Count, 0, magazineSize);
            var chamberSpace = magazineSize - chambered;
            return chamberSpace <= 0 ? 0 : Math.Min(box.Amount, chamberSpace);
        }

        var reserveSlot = ammoSlot - 1;
        if (reserveSlot >= _hardpointAmmo.GetMaxStoredRoundSlots(hardpointAmmo))
            return 0;

        var storedRounds = _hardpointAmmo.GetStoredSlotRounds(hardpointAmmo, reserveSlot, magazineSize);
        var reserveSpace = magazineSize - storedRounds;
        if (reserveSpace <= 0)
            return 0;

        return Math.Min(box.Amount, reserveSpace);
    }

    private int GetDirectUnloadAmount(
        BallisticAmmoProviderComponent ammo,
        RMCVehicleHardpointAmmoComponent hardpointAmmo,
        int ammoSlot)
    {
        if (ammoSlot < 0)
            return 0;

        var magazineSize = _hardpointAmmo.GetMagazineSize(hardpointAmmo, ammo);
        if (ammoSlot == 0)
            return Math.Min(Math.Clamp(ammo.Count, 0, magazineSize), ammo.UnspawnedCount);

        var reserveSlot = ammoSlot - 1;
        if (reserveSlot >= _hardpointAmmo.GetMaxStoredRoundSlots(hardpointAmmo))
            return 0;

        return _hardpointAmmo.GetStoredSlotRounds(hardpointAmmo, reserveSlot, magazineSize);
    }

    private void DoLoadAmmoSlot(
        Entity<RMCVehicleAmmoLoaderComponent> loader,
        EntityUid user,
        Entity<BulletBoxComponent> box,
        EntityUid ammoUid,
        BallisticAmmoProviderComponent ammo,
        RMCVehicleHardpointAmmoComponent hardpointAmmo,
        int ammoSlot)
    {
        var transferAmount = GetLoadAmount(box.Comp, ammo, hardpointAmmo, ammoSlot);
        if (transferAmount <= 0)
            return;

        if (!_bulletBox.TryConsume(box, transferAmount))
            return;

        if (box.Comp.Amount <= 0)
            Del(box.Owner);

        var magazineSize = _hardpointAmmo.GetMagazineSize(hardpointAmmo, ammo);
        if (ammoSlot == 0)
        {
            _gun.SetBallisticUnspawned((ammoUid, ammo), ammo.UnspawnedCount + transferAmount);
        }
        else
        {
            var reserveSlot = ammoSlot - 1;
            var storedRounds = _hardpointAmmo.GetStoredSlotRounds(hardpointAmmo, reserveSlot, magazineSize);
            _hardpointAmmo.SetStoredSlotRounds((ammoUid, hardpointAmmo), reserveSlot, storedRounds + transferAmount, magazineSize);
        }

        _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-loaded", ("amount", transferAmount), ("target", ammoUid)), loader, user);
    }

    private void DoDirectUnloadAmmoSlot(
        Entity<RMCVehicleAmmoLoaderComponent> loader,
        EntityUid user,
        EntityUid ammoUid,
        BallisticAmmoProviderComponent ammo,
        RMCVehicleHardpointAmmoComponent hardpointAmmo,
        RefillableByBulletBoxComponent refill,
        int ammoSlot)
    {
        var transferAmount = GetDirectUnloadAmount(ammo, hardpointAmmo, ammoSlot);
        if (transferAmount <= 0)
            return;

        var unloadedAmount = SpawnUnloadedAmmo(user, refill, transferAmount);
        if (unloadedAmount <= 0)
            return;

        var magazineSize = _hardpointAmmo.GetMagazineSize(hardpointAmmo, ammo);
        if (ammoSlot == 0)
        {
            _gun.SetBallisticUnspawned((ammoUid, ammo), ammo.UnspawnedCount - unloadedAmount);
            _hardpointAmmo.NormalizeAmmoQueue((ammoUid, hardpointAmmo), ammo);
        }
        else
        {
            var reserveSlot = ammoSlot - 1;
            var storedRounds = _hardpointAmmo.GetStoredSlotRounds(hardpointAmmo, reserveSlot, magazineSize);
            _hardpointAmmo.SetStoredSlotRounds((ammoUid, hardpointAmmo), reserveSlot, storedRounds - unloadedAmount, magazineSize);
        }

        _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-unloaded", ("amount", unloadedAmount), ("target", ammoUid)), loader, user);
    }

    private int SpawnUnloadedAmmo(EntityUid user, RefillableByBulletBoxComponent refill, int amount)
    {
        if (amount <= 0 || refill.BulletType is not { } prototype)
            return 0;

        var remaining = amount;
        var unloaded = 0;
        while (remaining > 0)
        {
            var spawned = Spawn(prototype, Transform(user).Coordinates);
            if (!TryComp(spawned, out BulletBoxComponent? box))
            {
                _hands.PickupOrDrop(user, spawned);
                return unloaded + 1;
            }

            var boxMax = Math.Max(1, box.Max);
            var boxAmount = Math.Min(remaining, boxMax);
            if (!_bulletBox.TrySetAmount((spawned, box), boxAmount))
            {
                Del(spawned);
                return unloaded;
            }

            _hands.PickupOrDrop(user, spawned);
            remaining -= boxAmount;
            unloaded += boxAmount;
        }

        return unloaded;
    }

    private bool TryGetTurretSlot(
        EntityUid vehicle,
        string parentSlotId,
        string childSlotId,
        ItemSlotsComponent itemSlots,
        out RMCHardpointSlot turretSlot,
        out EntityUid item)
    {
        turretSlot = default!;
        item = default;

        if (!_itemSlots.TryGetSlot(vehicle, parentSlotId, out var parentSlot, itemSlots) || !parentSlot.HasItem)
            return false;

        var turretUid = parentSlot.Item!.Value;
        if (!TryComp(turretUid, out RMCHardpointSlotsComponent? turretSlots) ||
            !TryComp(turretUid, out ItemSlotsComponent? turretItemSlots))
        {
            return false;
        }

        foreach (var slot in turretSlots.Slots)
        {
            if (!string.Equals(slot.Id, childSlotId, StringComparison.OrdinalIgnoreCase))
                continue;

            turretSlot = slot;
            if (!_itemSlots.TryGetSlot(turretUid, slot.Id, out var turretItemSlot, turretItemSlots) ||
                !turretItemSlot.HasItem)
            {
                return false;
            }

            item = turretItemSlot.Item!.Value;
            return true;
        }

        return false;
    }

    private bool TryGetAmmoProviderFromItem(
        EntityUid item,
        BulletBoxComponent box,
        out EntityUid ammoUid,
        out BallisticAmmoProviderComponent ammo,
        out RMCVehicleHardpointAmmoComponent hardpointAmmo,
        out bool wrongAmmo)
    {
        wrongAmmo = false;
        if (!TryGetAmmoProviderFromItem(item, out ammoUid, out ammo, out hardpointAmmo, out var refill))
            return false;

        if (refill.BulletType == box.BulletType)
            return true;

        wrongAmmo = true;
        return false;
    }

    private bool TryGetAmmoProviderFromItem(
        EntityUid item,
        out EntityUid ammoUid,
        out BallisticAmmoProviderComponent ammo,
        out RMCVehicleHardpointAmmoComponent hardpointAmmo,
        out RefillableByBulletBoxComponent refill)
    {
        ammoUid = default;
        ammo = default!;
        hardpointAmmo = default!;
        refill = default!;

        if (!TryComp(item, out BallisticAmmoProviderComponent? ammoProvider))
            return false;

        if (!TryComp(item, out RMCVehicleHardpointAmmoComponent? hardpointAmmoComp))
            return false;

        if (!TryComp(item, out RefillableByBulletBoxComponent? refillComp))
            return false;

        ammoUid = item;
        ammo = ammoProvider;
        hardpointAmmo = hardpointAmmoComp;
        refill = refillComp;
        return true;
    }

}

internal enum AmmoLoaderLookupResult : byte
{
    NotFound,
    WrongAmmo,
}

[Serializable, NetSerializable]
public sealed partial class VehicleAmmoLoaderDoAfterEvent : DoAfterEvent
{
    [DataField(required: true)]
    public string SlotId = string.Empty;

    [DataField]
    public int AmmoSlot;

    [DataField]
    public RMCVehicleAmmoLoaderSlotAction Action;

    public VehicleAmmoLoaderDoAfterEvent()
    {
    }

    public VehicleAmmoLoaderDoAfterEvent(string slotId, int ammoSlot, RMCVehicleAmmoLoaderSlotAction action)
    {
        SlotId = slotId;
        AmmoSlot = ammoSlot;
        Action = action;
    }

    public override DoAfterEvent Clone()
    {
        return new VehicleAmmoLoaderDoAfterEvent(SlotId, AmmoSlot, Action);
    }

    public override bool IsDuplicate(DoAfterEvent other)
    {
        return other is VehicleAmmoLoaderDoAfterEvent loaderEvent
               && loaderEvent.SlotId == SlotId
               && loaderEvent.AmmoSlot == AmmoSlot
               && loaderEvent.Action == Action;
    }
}
