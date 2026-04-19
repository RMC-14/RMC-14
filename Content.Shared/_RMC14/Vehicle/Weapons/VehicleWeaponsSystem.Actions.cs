using System.Collections.Generic;
using System.Linq;
using Content.Shared.Actions.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Vehicle.Components;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared._RMC14.Vehicle;

public sealed partial class VehicleWeaponsSystem
{
    private void OnHardpointActionSelect(Entity<VehicleWeaponsOperatorComponent> ent, ref VehicleHardpointSelectActionEvent args)
    {
        if (_net.IsClient || args.Handled)
            return;

        if (args.Performer == default || !Exists(args.Performer) || args.Performer != ent.Owner)
            return;

        if (!CanUseHardpointActions(args.Performer))
            return;

        if (!TryComp(args.Performer, out BuckleComponent? buckle) ||
            buckle.BuckledTo is not { } seat ||
            !HasComp<VehicleWeaponsSeatComponent>(seat))
        {
            return;
        }

        if (!TryComp(args.Action, out VehicleHardpointActionComponent? hardpointAction))
            return;

        if (TrySelectHardpoint(seat, args.Performer, hardpointAction.MountedWeapon, fromUi: false))
            args.Handled = true;
    }

    private void OnViewToggled(Entity<VehicleWeaponsOperatorComponent> ent, ref VehicleViewToggledEvent args)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.Vehicle is not { } vehicle ||
            !TryComp(vehicle, out VehicleWeaponsComponent? weapons))
        {
            return;
        }

        RefreshHardpointActions(ent.Owner, vehicle, weapons, ent.Comp);

        if (TryGetUserWeaponsSeat(ent.Owner, out var seat, out _))
            UpdateWeaponsUi(seat, vehicle, weapons, operatorUid: ent.Owner);
    }

    private readonly record struct HardpointActionSlot(EntityUid MountedWeapon, EntityUid IconEntity, string DisplayName);

    private void RefreshHardpointActions(
        EntityUid user,
        EntityUid vehicle,
        VehicleWeaponsComponent weapons,
        VehicleWeaponsOperatorComponent? operatorComp = null,
        HardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null)
    {
        if (_net.IsClient)
            return;

        if (!Resolve(user, ref operatorComp, logMissing: false))
            return;

        if (!Resolve(vehicle, ref hardpoints, ref itemSlots, logMissing: false))
        {
            ClearHardpointActions(user, operatorComp);
            return;
        }

        var desired = CanUseHardpointActions(user)
            ? GetSelectableHardpointActionSlots(vehicle, user, weapons, hardpoints, itemSlots)
            : new List<HardpointActionSlot>();

        var desiredSlots = new HashSet<EntityUid>(desired.Select(slot => slot.MountedWeapon));

        foreach (var pair in operatorComp.HardpointActions.ToArray())
        {
            if (!desiredSlots.Contains(pair.Key) || !Exists(pair.Value))
            {
                RemoveAndDeleteHardpointAction(user, pair.Value);
                operatorComp.HardpointActions.Remove(pair.Key);
            }
        }

        for (var i = 0; i < desired.Count; i++)
        {
            var desiredSlot = desired[i];
            if (operatorComp.HardpointActions.TryGetValue(desiredSlot.MountedWeapon, out var existingAction) &&
                Exists(existingAction) &&
                TryComp(existingAction, out ActionComponent? existingActionComp) &&
                existingActionComp.Container == desiredSlot.IconEntity)
            {
                if (TryComp(existingAction, out VehicleHardpointActionComponent? existingHardpointAction))
                {
                    existingHardpointAction.MountedWeapon = desiredSlot.MountedWeapon;
                    existingHardpointAction.SortOrder = i;
                    Dirty(existingAction, existingHardpointAction);
                }

                _actions.SetTemporary((existingAction, existingActionComp), false);
                _metaData.SetEntityName(existingAction, desiredSlot.DisplayName);
                continue;
            }

            if (operatorComp.HardpointActions.TryGetValue(desiredSlot.MountedWeapon, out var staleAction) &&
                Exists(staleAction))
            {
                RemoveAndDeleteHardpointAction(user, staleAction);
                operatorComp.HardpointActions.Remove(desiredSlot.MountedWeapon);
            }

            EntityUid? action = null;
            if (!_actions.AddAction(user, ref action, HardpointSelectActionId, container: desiredSlot.IconEntity) ||
                action == null)
            {
                continue;
            }

            var hardpointAction = EnsureComp<VehicleHardpointActionComponent>(action.Value);
            hardpointAction.MountedWeapon = desiredSlot.MountedWeapon;
            hardpointAction.SortOrder = i;
            Dirty(action.Value, hardpointAction);
            _actions.SetTemporary((action.Value, Comp<ActionComponent>(action.Value)), false);
            _metaData.SetEntityName(action.Value, desiredSlot.DisplayName);
            operatorComp.HardpointActions[desiredSlot.MountedWeapon] = action.Value;
        }

        UpdateHardpointActionStates(user, weapons, operatorComp);
    }

    private List<HardpointActionSlot> GetSelectableHardpointActionSlots(
        EntityUid vehicle,
        EntityUid user,
        VehicleWeaponsComponent weapons,
        HardpointSlotsComponent hardpoints,
        ItemSlotsComponent itemSlots)
    {
        var slots = new List<HardpointActionSlot>();
        if (!TryGetUserWeaponsSeat(user, out _, out var seatComp))
            return slots;

        foreach (var mountedSlot in _topology.GetMountedSlots(vehicle, hardpoints, itemSlots))
        {
            if (!IsHardpointTypeAllowed(seatComp, mountedSlot.HardpointType))
                continue;

            if (mountedSlot.Item is not { } installed ||
                !HasComp<VehicleTurretComponent>(installed) ||
                !HasComp<GunComponent>(installed))
            {
                continue;
            }

            var sharedSelection = IsSharedHardpointType(mountedSlot.HardpointType);
            if (sharedSelection ||
                !weapons.HardpointOperators.TryGetValue(installed, out var slotOperator) ||
                slotOperator == user)
            {
                slots.Add(new HardpointActionSlot(installed, installed, Name(installed)));
            }
        }

        return slots;
    }

    private void UpdateHardpointActionStates(
        EntityUid user,
        VehicleWeaponsComponent weapons,
        VehicleWeaponsOperatorComponent? operatorComp = null)
    {
        if (_net.IsClient || !Resolve(user, ref operatorComp, logMissing: false))
            return;

        var canUseHardpointActions = CanUseHardpointActions(user);
        EntityUid? selectedWeapon = weapons.OperatorSelections.TryGetValue(user, out var weapon)
            ? weapon
            : null;

        foreach (var pair in operatorComp.HardpointActions)
        {
            _actions.SetEnabled(pair.Value, canUseHardpointActions);
            _actions.SetToggled(
                pair.Value,
                canUseHardpointActions &&
                selectedWeapon != null &&
                pair.Key == selectedWeapon.Value);
        }
    }

    private void ClearHardpointActions(EntityUid user, VehicleWeaponsOperatorComponent? operatorComp = null)
    {
        if (_net.IsClient || !Resolve(user, ref operatorComp, logMissing: false))
            return;

        foreach (var action in operatorComp.HardpointActions.Values.ToArray())
        {
            if (Exists(action))
                RemoveAndDeleteHardpointAction(user, action);
        }

        operatorComp.HardpointActions.Clear();
    }

    private void RemoveAndDeleteHardpointAction(EntityUid user, EntityUid action)
    {
        if (!Exists(action))
            return;

        _actions.RemoveAction(user, action);

        if (Exists(action))
            QueueDel(action);
    }

    private bool CanUseHardpointActions(EntityUid user, bool forUi = false)
    {
        if (!TryGetUserWeaponsSeat(user, out _, out var seatComp))
            return false;

        if (forUi && !seatComp.AllowUiSelection)
            return false;

        if (!forUi && !seatComp.AllowHotbarSelection)
            return false;

        if (TryComp(user, out VehicleViewToggleComponent? viewToggle) && !viewToggle.IsOutside)
            return false;

        return true;
    }
}
