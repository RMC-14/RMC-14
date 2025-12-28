using System.Collections.Generic;
using System.Linq;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Containers;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCVehicleWeaponsSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly RMCVehicleSystem _vehicleSystem = default!;
    [Dependency] private readonly RMCVehicleViewToggleSystem _viewToggle = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleWeaponsSeatComponent, StrapAttemptEvent>(OnWeaponSeatStrapAttempt);
        SubscribeLocalEvent<VehicleWeaponsSeatComponent, StrappedEvent>(OnWeaponSeatStrapped);
        SubscribeLocalEvent<VehicleWeaponsSeatComponent, UnstrappedEvent>(OnWeaponSeatUnstrapped);

        SubscribeLocalEvent<VehicleWeaponsSeatComponent, BoundUIOpenedEvent>(OnWeaponsUiOpened);
        SubscribeLocalEvent<VehicleWeaponsSeatComponent, BoundUIClosedEvent>(OnWeaponsUiClosed);
        SubscribeLocalEvent<VehicleWeaponsSeatComponent, RMCVehicleWeaponsSelectMessage>(OnWeaponsSelect);

        SubscribeLocalEvent<RMCHardpointSlotsChangedEvent>(OnHardpointSlotsChanged);

        SubscribeLocalEvent<VehicleTurretComponent, GunShotEvent>(OnTurretGunShot);
    }

    private void OnWeaponSeatStrapAttempt(Entity<VehicleWeaponsSeatComponent> ent, ref StrapAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (_skills.HasSkills(args.Buckle.Owner, ent.Comp.Skills))
            return;

        if (args.Popup)
            _popup.PopupClient(Loc.GetString("rmc-skills-cant-operate", ("target", ent)), args.Buckle, args.User);
    }

    private void OnWeaponSeatStrapped(Entity<VehicleWeaponsSeatComponent> ent, ref StrappedEvent args)
    {
        if (_net.IsClient)
            return;

        if (!_vehicleSystem.TryGetVehicleFromInterior(ent.Owner, out var vehicle) || vehicle == null)
        {
            return;
        }

        var vehicleUid = vehicle.Value;
        var weapons = EnsureComp<RMCVehicleWeaponsComponent>(vehicleUid);
        weapons.Operator = args.Buckle.Owner;
        weapons.SelectedWeapon = null;
        ClearOperatorSelections(weapons, args.Buckle.Owner);
        Dirty(vehicleUid, weapons);

        var operatorComp = EnsureComp<VehicleWeaponsOperatorComponent>(args.Buckle.Owner);
        operatorComp.Vehicle = vehicle;
        Dirty(args.Buckle.Owner, operatorComp);

        if (HasComp<VehicleEnterComponent>(vehicleUid))
        {
            _eye.SetTarget(args.Buckle.Owner, vehicleUid);
            _viewToggle.EnableViewToggle(args.Buckle.Owner, vehicleUid, ent.Owner, insideTarget: null, isOutside: true);
        }

        _ui.OpenUi(ent.Owner, RMCVehicleWeaponsUiKey.Key, args.Buckle.Owner);
        UpdateWeaponsUi(ent.Owner, vehicleUid, weapons);
    }

    private void OnWeaponSeatUnstrapped(Entity<VehicleWeaponsSeatComponent> ent, ref UnstrappedEvent args)
    {
        if (_net.IsClient)
            return;

        if (!_vehicleSystem.TryGetVehicleFromInterior(ent.Owner, out var vehicle) || vehicle == null)
            return;

        var vehicleUid = vehicle.Value;
        if (TryComp(vehicleUid, out RMCVehicleWeaponsComponent? weapons) &&
            weapons.Operator == args.Buckle.Owner)
        {
            weapons.Operator = null;
            weapons.SelectedWeapon = null;
            ClearOperatorSelections(weapons, args.Buckle.Owner);
            Dirty(vehicleUid, weapons);
            UpdateWeaponsUi(ent.Owner, vehicleUid, weapons);
        }
        else if (TryComp(vehicleUid, out RMCVehicleWeaponsComponent? otherWeapons))
        {
            ClearOperatorSelections(otherWeapons, args.Buckle.Owner);
            UpdateWeaponsUi(ent.Owner, vehicleUid, otherWeapons);
        }

        RemCompDeferred<VehicleWeaponsOperatorComponent>(args.Buckle.Owner);
        _ui.CloseUi(ent.Owner, RMCVehicleWeaponsUiKey.Key, args.Buckle.Owner);

        if (TryComp(args.Buckle.Owner, out EyeComponent? eye) && eye.Target == vehicleUid)
            _eye.SetTarget(args.Buckle.Owner, null, eye);

        _viewToggle.DisableViewToggle(args.Buckle.Owner, ent.Owner);
    }

    private void OnWeaponsUiOpened(Entity<VehicleWeaponsSeatComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!Equals(args.UiKey, RMCVehicleWeaponsUiKey.Key))
            return;


        if (!_vehicleSystem.TryGetVehicleFromInterior(ent.Owner, out var vehicle) || vehicle == null)
            return;

        var vehicleUid = vehicle.Value;
        if (!TryComp(vehicleUid, out RMCVehicleWeaponsComponent? weapons))
            return;

        UpdateWeaponsUi(ent.Owner, vehicleUid, weapons);
    }

    private void OnWeaponsUiClosed(Entity<VehicleWeaponsSeatComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!Equals(args.UiKey, RMCVehicleWeaponsUiKey.Key))
            return;

        // No state cleanup yet; selection is tied to the vehicle operator.
    }

    private void OnWeaponsSelect(Entity<VehicleWeaponsSeatComponent> ent, ref RMCVehicleWeaponsSelectMessage args)
    {
        if (!Equals(args.UiKey, RMCVehicleWeaponsUiKey.Key))
            return;

        if (args.Actor == default || !Exists(args.Actor))
            return;

        if (!_vehicleSystem.TryGetVehicleFromInterior(ent.Owner, out var vehicle) || vehicle == null)
            return;

        var vehicleUid = vehicle.Value;
        if (!TryComp(vehicleUid, out RMCVehicleWeaponsComponent? weapons) || weapons.Operator != args.Actor)
            return;

        if (!TryComp(args.Actor, out BuckleComponent? buckle) || buckle.BuckledTo != ent.Owner)
            return;

        if (TryComp(args.Actor, out VehiclePortGunOperatorComponent? portGunOperator) &&
            portGunOperator.Gun != null)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-portgun-active"), ent, args.Actor);
            return;
        }

        if (!TryComp(vehicleUid, out RMCHardpointSlotsComponent? hardpoints) ||
            !TryComp(vehicleUid, out ItemSlotsComponent? itemSlots))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(args.SlotId) ||
            !_itemSlots.TryGetSlot(vehicleUid, args.SlotId, out var itemSlot, itemSlots) ||
            !itemSlot.HasItem)
        {
            ClearOperatorSelections(weapons, args.Actor);
            weapons.SelectedWeapon = null;
            Dirty(vehicleUid, weapons);
            UpdateWeaponsUi(ent.Owner, vehicleUid, weapons, hardpoints, itemSlots, args.Actor);
            return;
        }

        var item = itemSlot.Item!.Value;
        if (!HasComp<VehicleTurretComponent>(item) || !HasComp<GunComponent>(item))
        {
            ClearOperatorSelections(weapons, args.Actor);
            weapons.SelectedWeapon = null;
            Dirty(vehicleUid, weapons);
            UpdateWeaponsUi(ent.Owner, vehicleUid, weapons, hardpoints, itemSlots, args.Actor);
            return;
        }

        if (weapons.HardpointOperators.TryGetValue(args.SlotId, out var currentOperator) &&
            currentOperator != args.Actor)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-weapons-ui-hardpoint-in-use", ("operator", currentOperator)), ent, args.Actor);
            UpdateWeaponsUi(ent.Owner, vehicleUid, weapons, hardpoints, itemSlots, args.Actor);
            return;
        }

        if (weapons.OperatorSelections.TryGetValue(args.Actor, out var existingSlot) &&
            string.Equals(existingSlot, args.SlotId, StringComparison.OrdinalIgnoreCase))
        {
            weapons.OperatorSelections.Remove(args.Actor);
            weapons.HardpointOperators.Remove(args.SlotId);
            if (weapons.SelectedWeapon == item)
                weapons.SelectedWeapon = null;
        }
        else
        {
            if (existingSlot != null)
                weapons.HardpointOperators.Remove(existingSlot);

            weapons.OperatorSelections[args.Actor] = args.SlotId;
            weapons.HardpointOperators[args.SlotId] = args.Actor;
            weapons.SelectedWeapon = item;
        }

        Dirty(vehicleUid, weapons);
        UpdateWeaponsUi(ent.Owner, vehicleUid, weapons, hardpoints, itemSlots, args.Actor);
    }

    private void OnHardpointSlotsChanged(RMCHardpointSlotsChangedEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp(args.Vehicle, out RMCVehicleWeaponsComponent? weapons))
            return;

        RMCHardpointSlotsComponent? hardpoints = null;
        ItemSlotsComponent? itemSlots = null;

        if (weapons.SelectedWeapon is { } selected &&
            Resolve(args.Vehicle, ref hardpoints, logMissing: false) &&
            Resolve(args.Vehicle, ref itemSlots, logMissing: false) &&
            !IsSelectedWeaponInstalled(args.Vehicle, selected, hardpoints, itemSlots))
        {
            weapons.SelectedWeapon = null;
            Dirty(args.Vehicle, weapons);
        }

        PruneHardpointOperators(args.Vehicle, weapons, hardpoints, itemSlots);

        if (TryGetOperatorSeat(weapons, out var seat))
            UpdateWeaponsUi(seat, args.Vehicle, weapons, hardpoints, itemSlots);
    }

    private bool IsSelectedWeaponInstalled(EntityUid vehicle, EntityUid selected, RMCHardpointSlotsComponent hardpoints, ItemSlotsComponent itemSlots)
    {
        foreach (var slot in hardpoints.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            if (_itemSlots.TryGetSlot(vehicle, slot.Id, out var slotData, itemSlots) &&
                slotData.HasItem &&
                slotData.Item == selected)
            {
                return true;
            }
        }

        return false;
    }

    private void OnTurretGunShot(Entity<VehicleTurretComponent> ent, ref GunShotEvent args)
    {
        if (_net.IsClient)
            return;

        if (!_container.TryGetContainingContainer(ent.Owner, out var container))
            return;

        if (!TryComp(container.Owner, out RMCVehicleWeaponsComponent? weapons))
            return;

        if (TryGetOperatorSeat(weapons, out var seat))
            UpdateWeaponsUi(seat, container.Owner, weapons);
    }

    private void UpdateWeaponsUi(
        EntityUid seat,
        EntityUid vehicle,
        RMCVehicleWeaponsComponent? weapons = null,
        RMCHardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null,
        EntityUid? operatorUid = null)
    {
        if (_net.IsClient)
            return;

        if (!Resolve(vehicle, ref weapons, logMissing: false))
            return;

        if (!Resolve(vehicle, ref hardpoints, logMissing: false))
            return;

        if (!Resolve(vehicle, ref itemSlots, logMissing: false))
            return;

        if (operatorUid == null)
            operatorUid = weapons.Operator;

        string? operatorSlot = null;
        if (operatorUid != null &&
            weapons.OperatorSelections.TryGetValue(operatorUid.Value, out var operatorSelection))
        {
            operatorSlot = operatorSelection;
        }

        var entries = new List<RMCVehicleWeaponsUiEntry>(hardpoints.Slots.Count);

        foreach (var slot in hardpoints.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            var hasItem = _itemSlots.TryGetSlot(vehicle, slot.Id, out var itemSlot, itemSlots) && itemSlot.HasItem;
            EntityUid? item = hasItem ? itemSlot!.Item : null;
            string? installedName = null;
            NetEntity? installedEntity = null;

            if (item != null)
            {
                installedName = Name(item.Value);
                installedEntity = GetNetEntity(item.Value);
            }

            var operatorName = (string?) null;
            var operatorIsSelf = false;
            var hasOperator = weapons.HardpointOperators.TryGetValue(slot.Id, out var slotOperator);
            if (hasOperator)
            {
                operatorName = Name(slotOperator);
                operatorIsSelf = operatorUid != null && slotOperator == operatorUid.Value;
            }

            var selectable = item != null && HasComp<VehicleTurretComponent>(item.Value) && HasComp<GunComponent>(item.Value);
            if (selectable && hasOperator && !operatorIsSelf)
                selectable = false;

            var selected = operatorSlot != null && string.Equals(operatorSlot, slot.Id, StringComparison.OrdinalIgnoreCase);

            var ammoCount = 0;
            var ammoCapacity = 0;
            var hasAmmo = false;

            if (selectable && item != null)
            {
                var ammoEv = new GetAmmoCountEvent();
                RaiseLocalEvent(item.Value, ref ammoEv);
                ammoCount = ammoEv.Count;
                ammoCapacity = ammoEv.Capacity;
                hasAmmo = ammoEv.Capacity > 0;
            }

            var magazineSize = 0;
            var storedMagazines = 0;
            var maxStoredMagazines = 0;
            var hasMagazineData = false;

            if (item != null && TryComp(item.Value, out RMCVehicleHardpointAmmoComponent? hardpointAmmo))
            {
                magazineSize = Math.Max(1, hardpointAmmo.MagazineSize);
                storedMagazines = hardpointAmmo.StoredMagazines;
                maxStoredMagazines = hardpointAmmo.MaxStoredMagazines;
                hasMagazineData = hardpointAmmo.MagazineSize > 0 || hardpointAmmo.MaxStoredMagazines > 0;
            }

            entries.Add(new RMCVehicleWeaponsUiEntry(
                slot.Id,
                slot.HardpointType,
                installedName,
                installedEntity,
                hasItem,
                selectable,
                selected,
                ammoCount,
                ammoCapacity,
                hasAmmo,
                magazineSize,
                storedMagazines,
                maxStoredMagazines,
                hasMagazineData,
                operatorName,
                operatorIsSelf));
        }

        _ui.SetUiState(seat, RMCVehicleWeaponsUiKey.Key, new RMCVehicleWeaponsUiState(entries));
    }

    private void ClearOperatorSelections(RMCVehicleWeaponsComponent weapons, EntityUid operatorUid)
    {
        if (weapons.OperatorSelections.TryGetValue(operatorUid, out var slotId))
        {
            weapons.OperatorSelections.Remove(operatorUid);
            weapons.HardpointOperators.Remove(slotId);
        }

        foreach (var pair in weapons.HardpointOperators.ToArray())
        {
            if (pair.Value == operatorUid)
                weapons.HardpointOperators.Remove(pair.Key);
        }
    }

    private void PruneHardpointOperators(
        EntityUid vehicle,
        RMCVehicleWeaponsComponent weapons,
        RMCHardpointSlotsComponent? hardpoints,
        ItemSlotsComponent? itemSlots)
    {
        if (!Resolve(vehicle, ref hardpoints, logMissing: false))
            return;

        var validSlots = new HashSet<string>();
        foreach (var slot in hardpoints.Slots)
        {
            if (!string.IsNullOrWhiteSpace(slot.Id))
                validSlots.Add(slot.Id);
        }

        foreach (var entry in weapons.HardpointOperators.ToArray())
        {
            if (!validSlots.Contains(entry.Key))
            {
                weapons.HardpointOperators.Remove(entry.Key);
                continue;
            }

            if (!Exists(entry.Value))
                weapons.HardpointOperators.Remove(entry.Key);
        }

        foreach (var entry in weapons.OperatorSelections.ToArray())
        {
            if (!validSlots.Contains(entry.Value))
                weapons.OperatorSelections.Remove(entry.Key);
        }
    }

    private bool TryGetOperatorSeat(RMCVehicleWeaponsComponent weapons, out EntityUid seat)
    {
        seat = default;
        if (weapons.Operator is not { } operatorUid)
            return false;

        if (!TryComp(operatorUid, out BuckleComponent? buckle) || buckle.BuckledTo == null)
            return false;

        if (!HasComp<VehicleWeaponsSeatComponent>(buckle.BuckledTo.Value))
            return false;

        seat = buckle.BuckledTo.Value;
        return true;
    }
}
