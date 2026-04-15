using System;
using System.Collections.Generic;
using Content.Shared._RMC14.Weapons.Ranged.Ammo.BulletBox;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
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

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCVehicleAmmoLoaderComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCVehicleAmmoLoaderComponent, VehicleAmmoLoaderDoAfterEvent>(OnLoadDoAfter);
        SubscribeLocalEvent<RMCVehicleAmmoLoaderComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<RMCVehicleAmmoLoaderComponent, BoundUIClosedEvent>(OnUiClosed);
        SubscribeLocalEvent<RMCVehicleAmmoLoaderComponent, RMCVehicleAmmoLoaderSelectMessage>(OnUiSelect);
    }

    private void OnInteractUsing(Entity<RMCVehicleAmmoLoaderComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || _net.IsClient)
            return;

        if (!TryComp(args.Used, out BulletBoxComponent? box))
            return;

        if (!TryComp(ent.Owner, out UserInterfaceComponent? uiComp) ||
            !_ui.HasUi(ent.Owner, RMCVehicleAmmoLoaderUiKey.Key, uiComp))
        {
            return;
        }

        if (!_vehicleSystem.TryGetVehicleFromInterior(ent.Owner, out var vehicleUid) || vehicleUid == null)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-no-vehicle"), ent, args.User);
            return;
        }

        if (ent.Comp.BulletType != null && ent.Comp.BulletType != box.BulletType)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-wrong-ammo"), ent, args.User);
            return;
        }

        if (!TryComp(vehicleUid.Value, out RMCHardpointSlotsComponent? hardpoints) ||
            !TryComp(vehicleUid.Value, out ItemSlotsComponent? itemSlots))
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-no-hardpoint"), ent, args.User);
            return;
        }

        _ = TryFindAmmoProvider(vehicleUid.Value, hardpoints, itemSlots, ent.Comp, box, null, out _, out _, out _);

        if (!CanOpenUi(ent.Owner, args.User))
            return;

        TrySetActiveAmmoBox(ent.Owner, args.User, args.Used);

        _ui.OpenUi(ent.Owner, RMCVehicleAmmoLoaderUiKey.Key, args.User);
        UpdateUi(ent.Owner, box);
        args.Handled = true;
    }

    private void OnLoadDoAfter(Entity<RMCVehicleAmmoLoaderComponent> ent, ref VehicleAmmoLoaderDoAfterEvent args)
    {
        if (_net.IsClient || args.Cancelled || args.Handled || args.Used is not { } used)
            return;

        if (!TryComp(used, out BulletBoxComponent? box))
            return;

        if (!TryGetActiveAmmoBox(ent.Owner, args.User, out var activeBox) || activeBox != used)
            return;

        if (string.IsNullOrWhiteSpace(args.SlotId))
            return;

        if (!TryGetLoadableAmmoProvider(ent, args.User, box, args.SlotId, out _, out var ammoUid, out var ammo, out var hardpointAmmo))
            return;

        if (args.Action == RMCVehicleAmmoLoaderSlotAction.Load)
            DoLoadAmmoSlot(ent, args.User, (used, box), ammoUid, ammo, hardpointAmmo, args.AmmoSlot);
        else
            DoUnloadAmmoSlot(ent, args.User, (used, box), ammoUid, ammo, hardpointAmmo, args.AmmoSlot);

        UpdateUi(ent.Owner, box);
        args.Handled = true;
    }

    private void OnUiOpened(Entity<RMCVehicleAmmoLoaderComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!Equals(args.UiKey, RMCVehicleAmmoLoaderUiKey.Key))
            return;

        if (!_net.IsClient && TryGetActiveAmmoBox(ent.Owner, args.Actor, out var boxUid) &&
            TryComp(boxUid, out BulletBoxComponent? box))
        {
            UpdateUi(ent.Owner, box);
        }
    }

    private void OnUiClosed(Entity<RMCVehicleAmmoLoaderComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!Equals(args.UiKey, RMCVehicleAmmoLoaderUiKey.Key))
            return;

        ClearActiveAmmoBox(ent.Owner, args.Actor);
    }

    private void OnUiSelect(Entity<RMCVehicleAmmoLoaderComponent> ent, ref RMCVehicleAmmoLoaderSelectMessage args)
    {
        if (!Equals(args.UiKey, RMCVehicleAmmoLoaderUiKey.Key))
            return;

        if (args.Actor == default || !Exists(args.Actor))
            return;

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

        if (args.Action == RMCVehicleAmmoLoaderSlotAction.Unload &&
            GetUnloadAmount(box, ammo, hardpointAmmo, args.AmmoSlot) <= 0)
        {
            var popup = box.Amount >= box.Max
                ? Loc.GetString("rmc-vehicle-ammo-loader-box-full", ("box", box.Owner))
                : Loc.GetString("rmc-vehicle-ammo-loader-empty", ("box", ammoUid));
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

        if (!TryFindAmmoProvider(vehicle, hardpoints, itemSlots, loader.Comp, box, slotId, out ammoUid, out ammo, out hardpointAmmo))
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
        out RMCVehicleHardpointAmmoComponent hardpointAmmo)
    {
        ammoUid = default;
        ammo = default!;
        hardpointAmmo = default!;

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

            return TryGetAmmoProviderFromItem(item, box, out ammoUid, out ammo, out hardpointAmmo);
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
            if (TryGetAmmoProviderFromItem(item, box, out ammoUid, out ammo, out hardpointAmmo))
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
                if (TryGetAmmoProviderFromItem(turretItem, box, out ammoUid, out ammo, out hardpointAmmo))
                    return true;
            }
        }

        return false;
    }

    private void UpdateUi(EntityUid loader, BulletBoxComponent box)
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
            AppendTurretAmmoEntries(entries, item, slot.Id, loaderComp, box);

            if (!TryComp(item, out BallisticAmmoProviderComponent? ammoProvider))
                continue;

            if (!TryComp(item, out RMCVehicleHardpointAmmoComponent? hardpointAmmo))
                continue;

            if (!TryComp(item, out RefillableByBulletBoxComponent? refill) ||
                refill.BulletType != box.BulletType)
            {
                continue;
            }

            var magazineSize = _hardpointAmmo.GetMagazineSize(hardpointAmmo, ammoProvider);
            var ammoSlots = GetAmmoSlotUiEntries(box, ammoProvider, hardpointAmmo, magazineSize);
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
                magazineSize,
                ammoSlots,
                canLoad,
                canUnload));
        }

        _ui.SetUiState(loader, RMCVehicleAmmoLoaderUiKey.Key, new RMCVehicleAmmoLoaderUiState(entries, box.Amount, box.Max, box.BulletType));
    }

    private void AppendTurretAmmoEntries(
        List<RMCVehicleAmmoLoaderUiEntry> entries,
        EntityUid turretUid,
        string parentSlotId,
        RMCVehicleAmmoLoaderComponent loaderComp,
        BulletBoxComponent box)
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
            if (!TryGetAmmoProviderFromItem(item, box, out _, out var ammoProvider, out var hardpointAmmo))
                continue;

            var magazineSize = _hardpointAmmo.GetMagazineSize(hardpointAmmo, ammoProvider);
            var ammoSlots = GetAmmoSlotUiEntries(box, ammoProvider, hardpointAmmo, magazineSize);
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
                magazineSize,
                ammoSlots,
                canLoad,
                canUnload));
        }
    }

    private List<RMCVehicleAmmoLoaderUiAmmoSlot> GetAmmoSlotUiEntries(
        BulletBoxComponent box,
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
            HasLoadSpace(ammo, hardpointAmmo, 0),
            HasUnloadRounds(ammo, hardpointAmmo, 0),
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
                HasLoadSpace(ammo, hardpointAmmo, slotIndex),
                HasUnloadRounds(ammo, hardpointAmmo, slotIndex),
                false));
        }

        return entries;
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

    private int GetUnloadAmount(
        BulletBoxComponent box,
        BallisticAmmoProviderComponent ammo,
        RMCVehicleHardpointAmmoComponent hardpointAmmo,
        int ammoSlot)
    {
        if (ammoSlot < 0)
            return 0;

        var boxSpace = box.Max - box.Amount;
        if (boxSpace <= 0)
            return 0;

        var magazineSize = _hardpointAmmo.GetMagazineSize(hardpointAmmo, ammo);
        if (ammoSlot == 0)
        {
            var removable = Math.Min(Math.Clamp(ammo.Count, 0, magazineSize), ammo.UnspawnedCount);
            return Math.Min(boxSpace, removable);
        }

        var reserveSlot = ammoSlot - 1;
        if (reserveSlot >= _hardpointAmmo.GetMaxStoredRoundSlots(hardpointAmmo))
            return 0;

        var storedRounds = _hardpointAmmo.GetStoredSlotRounds(hardpointAmmo, reserveSlot, magazineSize);
        return Math.Min(boxSpace, storedRounds);
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

    private void DoUnloadAmmoSlot(
        Entity<RMCVehicleAmmoLoaderComponent> loader,
        EntityUid user,
        Entity<BulletBoxComponent> box,
        EntityUid ammoUid,
        BallisticAmmoProviderComponent ammo,
        RMCVehicleHardpointAmmoComponent hardpointAmmo,
        int ammoSlot)
    {
        var transferAmount = GetUnloadAmount(box.Comp, ammo, hardpointAmmo, ammoSlot);
        if (transferAmount <= 0)
            return;

        if (!_bulletBox.TryAdd(box, transferAmount))
            return;

        var magazineSize = _hardpointAmmo.GetMagazineSize(hardpointAmmo, ammo);
        if (ammoSlot == 0)
        {
            _gun.SetBallisticUnspawned((ammoUid, ammo), ammo.UnspawnedCount - transferAmount);
        }
        else
        {
            var reserveSlot = ammoSlot - 1;
            var storedRounds = _hardpointAmmo.GetStoredSlotRounds(hardpointAmmo, reserveSlot, magazineSize);
            _hardpointAmmo.SetStoredSlotRounds((ammoUid, hardpointAmmo), reserveSlot, storedRounds - transferAmount, magazineSize);
        }

        _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-unloaded", ("amount", transferAmount), ("target", ammoUid)), loader, user);
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
        out RMCVehicleHardpointAmmoComponent hardpointAmmo)
    {
        ammoUid = default;
        ammo = default!;
        hardpointAmmo = default!;

        if (!TryComp(item, out BallisticAmmoProviderComponent? ammoProvider))
            return false;

        if (!TryComp(item, out RMCVehicleHardpointAmmoComponent? hardpointAmmoComp))
            return false;

        if (!TryComp(item, out RefillableByBulletBoxComponent? refill) ||
            refill.BulletType != box.BulletType)
        {
            return false;
        }

        ammoUid = item;
        ammo = ammoProvider;
        hardpointAmmo = hardpointAmmoComp;
        return true;
    }

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
