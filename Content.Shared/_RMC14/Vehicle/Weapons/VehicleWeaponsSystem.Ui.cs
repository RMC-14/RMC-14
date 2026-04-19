using System;
using System.Collections.Generic;
using Content.Shared.Buckle.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.UserInterface;
using Content.Shared.Vehicle.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Vehicle;

public sealed partial class VehicleWeaponsSystem
{
    private void OnWeaponsUiOpened(Entity<VehicleWeaponsSeatComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!Equals(args.UiKey, VehicleWeaponsUiKey.Key))
            return;

        if (!_vehicleSystem.TryGetVehicleFromInterior(ent.Owner, out var vehicle) || vehicle == null)
            return;

        var vehicleUid = vehicle.Value;
        if (!TryComp(vehicleUid, out VehicleWeaponsComponent? weapons))
            return;

        var viewer = args.Actor;
        if (viewer == default || !Exists(viewer))
            return;

        UpdateWeaponsUi(ent.Owner, vehicleUid, weapons, operatorUid: viewer);
    }

    private void OnWeaponsUiClosed(Entity<VehicleWeaponsSeatComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!Equals(args.UiKey, VehicleWeaponsUiKey.Key))
            return;
    }

    private void OnWeaponsSelect(Entity<VehicleWeaponsSeatComponent> ent, ref VehicleWeaponsSelectMessage args)
    {
        if (!Equals(args.UiKey, VehicleWeaponsUiKey.Key))
            return;

        if (args.Actor == default || !Exists(args.Actor))
            return;

        if (!CanUseHardpointActions(args.Actor, forUi: true))
            return;

        var mountedWeapon = args.MountedEntity != null
            ? GetEntity(args.MountedEntity.Value)
            : (EntityUid?) null;

        TrySelectHardpoint(ent.Owner, args.Actor, mountedWeapon, fromUi: true);
    }

    private void OnWeaponsStabilization(Entity<VehicleWeaponsSeatComponent> ent, ref VehicleWeaponsStabilizationMessage args)
    {
        if (!Equals(args.UiKey, VehicleWeaponsUiKey.Key))
            return;

        if (args.Actor == default || !Exists(args.Actor))
            return;

        if (!_vehicleSystem.TryGetVehicleFromInterior(ent.Owner, out var vehicle) || vehicle == null)
            return;

        var vehicleUid = vehicle.Value;
        if (!TryComp(vehicleUid, out VehicleWeaponsComponent? weapons) || weapons.Operator != args.Actor)
            return;

        if (!TryComp(args.Actor, out BuckleComponent? buckle) || buckle.BuckledTo != ent.Owner)
            return;

        HardpointSlotsComponent? hardpoints = null;
        ItemSlotsComponent? itemSlots = null;
        if (!Resolve(vehicleUid, ref hardpoints, logMissing: false) ||
            !Resolve(vehicleUid, ref itemSlots, logMissing: false))
        {
            return;
        }

        if (!weapons.OperatorSelections.TryGetValue(args.Actor, out var selectedWeapon) ||
            !Exists(selectedWeapon))
        {
            return;
        }

        if (!TryComp(selectedWeapon, out VehicleTurretComponent? turret) ||
            !_turretSystem.TryResolveRotationTarget(selectedWeapon, out var targetUid, out var targetTurret))
        {
            return;
        }

        if (!targetTurret.RotateToCursor)
            return;

        targetTurret.StabilizedRotation = args.Enabled;
        var vehicleRot = _transform.GetWorldRotation(vehicleUid);
        var currentWorld = (targetTurret.WorldRotation + vehicleRot).Reduced();
        if (args.Enabled)
            targetTurret.TargetRotation = currentWorld;
        else
            targetTurret.TargetRotation = targetTurret.WorldRotation;
        Dirty(targetUid, targetTurret);

        UpdateWeaponsUiForAllOperators(vehicleUid, weapons, hardpoints, itemSlots);
    }

    private void OnWeaponsAutoMode(Entity<VehicleWeaponsSeatComponent> ent, ref VehicleWeaponsAutoModeMessage args)
    {
        if (!Equals(args.UiKey, VehicleWeaponsUiKey.Key))
            return;

        if (args.Actor == default || !Exists(args.Actor))
            return;

        if (!_vehicleSystem.TryGetVehicleFromInterior(ent.Owner, out var vehicle) || vehicle == null)
            return;

        var vehicleUid = vehicle.Value;
        if (!TryComp(vehicleUid, out VehicleWeaponsComponent? weapons) || weapons.Operator != args.Actor)
            return;

        if (!TryComp(args.Actor, out BuckleComponent? buckle) || buckle.BuckledTo != ent.Owner)
            return;

        if (!TryComp(vehicleUid, out VehicleDeployableComponent? deployable))
            return;

        deployable.AutoTurretEnabled = args.Enabled;
        Dirty(vehicleUid, deployable);

        UpdateWeaponsUiForAllOperators(vehicleUid, weapons);
    }

    private void UpdateWeaponsUi(
        EntityUid seat,
        EntityUid vehicle,
        VehicleWeaponsComponent? weapons = null,
        HardpointSlotsComponent? hardpoints = null,
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

        VehicleWeaponsSeatComponent? operatorSeatComp = null;
        if (operatorUid != null)
            TryGetUserWeaponsSeat(operatorUid.Value, out _, out operatorSeatComp);

        EntityUid? operatorSelection = null;
        if (operatorUid != null &&
            weapons.OperatorSelections.TryGetValue(operatorUid.Value, out var selectedWeapon))
        {
            operatorSelection = selectedWeapon;
        }

        if (operatorSelection == null &&
            operatorUid != null &&
            operatorSeatComp != null &&
            !operatorSeatComp.AllowUiSelection &&
            weapons.Operator is { } primaryOperator &&
            weapons.OperatorSelections.TryGetValue(primaryOperator, out var primarySelection))
        {
            operatorSelection = primarySelection;
        }

        var mountedSlots = _topology.GetMountedSlots(vehicle, hardpoints, itemSlots);
        var entries = new List<VehicleWeaponsUiEntry>(mountedSlots.Count);
        var canUseHardpointActions = operatorUid == null || CanUseHardpointActions(operatorUid.Value, forUi: true);

        foreach (var mountedSlot in mountedSlots)
        {
            entries.Add(CreateMountedSlotUiEntry(
                mountedSlot,
                weapons,
                operatorUid,
                operatorSelection,
                canUseHardpointActions,
                operatorSeatComp));
        }

        var canToggleStabilization = false;
        var stabilizationEnabled = false;
        var canToggleAuto = false;
        var autoEnabled = false;

        var canControlToggles = operatorUid != null && weapons.Operator == operatorUid;
        if (canControlToggles &&
            operatorSelection != null &&
            Exists(operatorSelection.Value) &&
            TryComp(operatorSelection.Value, out VehicleTurretComponent? selectedTurret) &&
            _turretSystem.TryResolveRotationTarget(operatorSelection.Value, out _, out var targetTurret))
        {
            stabilizationEnabled = targetTurret.StabilizedRotation;
            canToggleStabilization = targetTurret.RotateToCursor;
        }

        if (canControlToggles &&
            TryComp(vehicle, out VehicleDeployableComponent? deployable))
        {
            canToggleAuto = true;
            autoEnabled = deployable.AutoTurretEnabled;
        }

        _ui.SetUiState(seat, VehicleWeaponsUiKey.Key,
            new VehicleWeaponsUiState(
                GetNetEntity(vehicle),
                entries,
                canToggleStabilization,
                stabilizationEnabled,
                canToggleAuto,
                autoEnabled));
    }

    private VehicleWeaponsUiEntry CreateMountedSlotUiEntry(
        VehicleMountedSlot mountedSlot,
        VehicleWeaponsComponent weapons,
        EntityUid? operatorUid,
        EntityUid? operatorSelection,
        bool canUseHardpointActions,
        VehicleWeaponsSeatComponent? operatorSeatComp)
    {
        var slotAllowed = operatorSeatComp == null || IsHardpointTypeAllowed(operatorSeatComp, mountedSlot.HardpointType);
        var sharedSelection = IsSharedHardpointType(mountedSlot.HardpointType);
        var hasItem = mountedSlot.Item != null;
        var item = mountedSlot.Item;
        string? installedName = null;
        NetEntity? installedEntity = null;

        if (item != null)
        {
            installedName = Name(item.Value);
            installedEntity = GetNetEntity(item.Value);
        }

        var operatorName = (string?) null;
        var operatorIsSelf = false;
        EntityUid slotOperator = default;
        var hasOperator = item != null && weapons.HardpointOperators.TryGetValue(item.Value, out slotOperator);
        if (hasOperator)
        {
            operatorName = Name(slotOperator);
            operatorIsSelf = operatorUid != null && slotOperator == operatorUid.Value;
        }

        var selectable = canUseHardpointActions &&
                         slotAllowed &&
                         item != null &&
                         HasComp<VehicleTurretComponent>(item.Value);
        if (selectable && hasOperator && !operatorIsSelf && !sharedSelection)
            selectable = false;

        var selected = item != null &&
                       operatorSelection != null &&
                       operatorSelection.Value == item.Value;

        var ammoCount = 0;
        var ammoCapacity = 0;
        var hasAmmo = false;
        var cooldownRemaining = 0f;
        var cooldownTotal = 0f;
        var isOnCooldown = false;

        if (item != null && TryComp(item.Value, out GunComponent? gun))
        {
            var ammoEv = new GetAmmoCountEvent();
            RaiseLocalEvent(item.Value, ref ammoEv);
            ammoCount = ammoEv.Count;
            ammoCapacity = ammoEv.Capacity;
            hasAmmo = ammoEv.Capacity > 0;

            if (gun.FireRateModified > 0f)
                cooldownTotal = 1f / gun.FireRateModified;

            var remaining = gun.NextFire - _timing.CurTime;
            if (remaining > TimeSpan.Zero)
            {
                cooldownRemaining = (float) remaining.TotalSeconds;
                isOnCooldown = cooldownRemaining > 0.001f;
            }
        }

        var magazineSize = 0;
        var storedMagazines = 0;
        var maxStoredMagazines = 0;
        var hasMagazineData = false;
        var integrity = 0f;
        var maxIntegrity = 0f;
        var hasIntegrity = false;

        if (item != null && TryComp(item.Value, out VehicleHardpointAmmoComponent? hardpointAmmo))
        {
            magazineSize = Math.Max(1, hardpointAmmo.MagazineSize);
            if (TryComp(item.Value, out BallisticAmmoProviderComponent? ammoProvider))
                magazineSize = _hardpointAmmo.GetMagazineSize(hardpointAmmo, ammoProvider);

            storedMagazines = _hardpointAmmo.GetStoredRounds(hardpointAmmo, magazineSize);
            maxStoredMagazines = _hardpointAmmo.GetMaxStoredRounds(hardpointAmmo, magazineSize);
            hasMagazineData = hardpointAmmo.MagazineSize > 0 || hardpointAmmo.MaxStoredMagazines > 0;
        }

        if (item != null && TryComp(item.Value, out HardpointIntegrityComponent? hardpointIntegrity))
        {
            integrity = hardpointIntegrity.Integrity;
            maxIntegrity = hardpointIntegrity.MaxIntegrity;
            hasIntegrity = true;
        }

        return new VehicleWeaponsUiEntry(
            mountedSlot.CompositeId,
            mountedSlot.HardpointType,
            installedEntity,
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
            operatorIsSelf,
            integrity,
            maxIntegrity,
            hasIntegrity,
            cooldownRemaining,
            cooldownTotal,
            isOnCooldown);
    }

    private void UpdateWeaponsUiForAllOperators(
        EntityUid vehicle,
        VehicleWeaponsComponent weapons,
        HardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null,
        bool refreshActions = false)
    {
        var query = EntityQueryEnumerator<VehicleWeaponsOperatorComponent>();
        while (query.MoveNext(out var operatorUid, out var operatorComp))
        {
            if (operatorComp.Vehicle != vehicle)
                continue;

            if (!TryGetUserWeaponsSeat(operatorUid, out var seat, out _))
                continue;

            if (refreshActions)
                RefreshHardpointActions(operatorUid, vehicle, weapons, operatorComp, hardpoints, itemSlots);

            UpdateWeaponsUi(seat, vehicle, weapons, hardpoints, itemSlots, operatorUid);
        }
    }
}
