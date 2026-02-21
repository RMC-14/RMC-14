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

        if (box.Amount <= 0)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-empty", ("box", box.Owner)), ent, args.User);
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

        if (!CanLoad(ent, args.User, box, args.SlotId, out _, out var ammoUid, out var ammo, out var hardpointAmmo))
            return;

        var magazineSize = Math.Max(1, hardpointAmmo.MagazineSize);
        if (box.Amount < magazineSize)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-not-enough"), ent, args.User);
            return;
        }

        if (!_bulletBox.TryConsume((used, box), magazineSize))
            return;

        var chambered = ammo.Count;
        if (chambered == 0)
        {
            var chamberSize = Math.Min(magazineSize, ammo.Capacity);
            _gun.SetBallisticUnspawned((ammoUid, ammo), chamberSize);
        }
        else
        {
            hardpointAmmo.StoredMagazines++;
            Dirty(ammoUid, hardpointAmmo);
        }

        _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-loaded", ("amount", magazineSize), ("target", ammoUid)), ent, args.User);
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

        if (!_hands.TryGetActiveItem(args.Actor, out var activeItem))
            return;

        if (!TryGetActiveAmmoBox(ent.Owner, args.Actor, out var boxUid) || activeItem != boxUid)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-hold-ammo"), ent, args.Actor);
            return;
        }

        if (!TryComp(boxUid, out BulletBoxComponent? box))
            return;

        if (!CanLoad(ent, args.Actor, box, args.SlotId, out _, out var ammoUid, out var ammo, out var hardpointAmmo))
            return;

        var magazineSize = Math.Max(1, hardpointAmmo.MagazineSize);
        var chambered = ammo.Count;
        var canStore = hardpointAmmo.StoredMagazines < hardpointAmmo.MaxStoredMagazines;

        if (box.Amount < magazineSize)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-not-enough"), ent, args.Actor);
            return;
        }

        if (chambered > 0 && !canStore)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-full", ("target", ammoUid)), ent, args.Actor);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.Actor, ent.Comp.LoadDelay, new VehicleAmmoLoaderDoAfterEvent(args.SlotId), ent.Owner, ent.Owner, boxUid)
        {
            BreakOnMove = true,
            BreakOnDropItem = true,
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

    private bool CanLoad(
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

        if (box.Amount <= 0)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-empty", ("box", box.Owner)), loader, user);
            return false;
        }

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

        var chambered = ammo.Count;
        var canStore = hardpointAmmo.StoredMagazines < hardpointAmmo.MaxStoredMagazines;
        if (chambered > 0 && !canStore)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-full", ("target", ammoUid)), loader, user);
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
            if (!TryComp(item, out BallisticAmmoProviderComponent? ammoProvider))
                continue;

            if (!TryComp(item, out RMCVehicleHardpointAmmoComponent? hardpointAmmo))
                continue;

            if (!TryComp(item, out RefillableByBulletBoxComponent? refill) ||
                refill.BulletType != box.BulletType)
            {
                continue;
            }

            var chambered = ammoProvider.Count;
            var magazineSize = Math.Max(1, hardpointAmmo.MagazineSize);
            var canLoad = box.Amount >= magazineSize &&
                          (chambered == 0 || hardpointAmmo.StoredMagazines < hardpointAmmo.MaxStoredMagazines);

            var name = Name(item);
            entries.Add(new RMCVehicleAmmoLoaderUiEntry(
                slot.Id,
                slot.HardpointType,
                name,
                GetNetEntity(item),
                chambered,
                magazineSize,
                hardpointAmmo.StoredMagazines,
                hardpointAmmo.MaxStoredMagazines,
                canLoad));

            AppendTurretAmmoEntries(entries, item, slot.Id, loaderComp, box);
        }

        _ui.SetUiState(loader, RMCVehicleAmmoLoaderUiKey.Key, new RMCVehicleAmmoLoaderUiState(entries, box.Amount, box.Max));
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

            var chambered = ammoProvider.Count;
            var magazineSize = Math.Max(1, hardpointAmmo.MagazineSize);
            var canLoad = box.Amount >= magazineSize &&
                          (chambered == 0 || hardpointAmmo.StoredMagazines < hardpointAmmo.MaxStoredMagazines);

            entries.Add(new RMCVehicleAmmoLoaderUiEntry(
                RMCVehicleTurretSlotIds.Compose(parentSlotId, turretSlot.Id),
                turretSlot.HardpointType,
                Name(item),
                GetNetEntity(item),
                chambered,
                magazineSize,
                hardpointAmmo.StoredMagazines,
                hardpointAmmo.MaxStoredMagazines,
                canLoad));
        }
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

    public VehicleAmmoLoaderDoAfterEvent()
    {
    }

    public VehicleAmmoLoaderDoAfterEvent(string slotId)
    {
        SlotId = slotId;
    }

    public override DoAfterEvent Clone()
    {
        return new VehicleAmmoLoaderDoAfterEvent(SlotId);
    }

    public override bool IsDuplicate(DoAfterEvent other)
    {
        return other is VehicleAmmoLoaderDoAfterEvent loaderEvent
               && loaderEvent.SlotId == SlotId
               && other.User == User
               && other.Target == Target
               && other.Used == Used;
    }
}
