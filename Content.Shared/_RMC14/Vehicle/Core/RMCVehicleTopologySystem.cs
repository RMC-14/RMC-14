using System;
using System.Collections.Generic;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Shared._RMC14.Vehicle;

public readonly record struct RMCVehicleMountedSlot(
    EntityUid Vehicle,
    EntityUid SlotOwner,
    string SlotId,
    string CompositeId,
    string HardpointType,
    EntityUid? Item,
    EntityUid? ParentItem,
    string? ParentSlotId)
{
    public bool HasItem => Item != null;
    public bool IsNested => ParentItem != null;
}

public sealed class RMCVehicleTopologySystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    public bool TryGetVehicle(EntityUid uid, out EntityUid vehicle, bool includeSelf = true)
    {
        return TryGetContainerAncestor<VehicleComponent>(uid, out vehicle, includeSelf);
    }

    public bool TryGetParentTurret(EntityUid uid, out EntityUid turret, bool includeSelf = false)
    {
        return TryGetContainerAncestor<VehicleTurretComponent>(uid, out turret, includeSelf);
    }

    public List<RMCVehicleMountedSlot> GetMountedSlots(
        EntityUid vehicle,
        RMCHardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null)
    {
        var result = new List<RMCVehicleMountedSlot>();

        if (!Resolve(vehicle, ref hardpoints, ref itemSlots, logMissing: false))
            return result;

        EnumerateMountedSlots(
            vehicle,
            vehicle,
            hardpoints,
            itemSlots,
            result,
            parentCompositeId: null,
            parentItem: null);

        return result;
    }

    public HashSet<string> GetMountedSlotIds(
        EntityUid vehicle,
        RMCHardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null)
    {
        var result = new HashSet<string>();

        if (!Resolve(vehicle, ref hardpoints, ref itemSlots, logMissing: false))
            return result;

        PopulateMountedSlotIds(vehicle, hardpoints, itemSlots, result, parentCompositeId: null);
        return result;
    }

    public bool TryGetMountedSlot(
        EntityUid vehicle,
        string slotId,
        out RMCVehicleMountedSlot mountedSlot,
        RMCHardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null)
    {
        mountedSlot = default;

        if (string.IsNullOrWhiteSpace(slotId) ||
            !Resolve(vehicle, ref hardpoints, ref itemSlots, logMissing: false))
        {
            return false;
        }

        return TryGetMountedSlotRecursive(
            vehicle,
            vehicle,
            slotId,
            hardpoints,
            itemSlots,
            parentCompositeId: null,
            parentItem: null,
            out mountedSlot);
    }

    public bool TryGetMountedSlotByItem(
        EntityUid vehicle,
        EntityUid item,
        out RMCVehicleMountedSlot mountedSlot,
        RMCHardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null)
    {
        mountedSlot = default;

        if (!Resolve(vehicle, ref hardpoints, ref itemSlots, logMissing: false))
            return false;

        return TryGetMountedSlotByItemRecursive(
            vehicle,
            vehicle,
            item,
            hardpoints,
            itemSlots,
            parentCompositeId: null,
            parentItem: null,
            out mountedSlot);
    }

    public bool TryGetMountedSlotItem(
        EntityUid vehicle,
        string slotId,
        out EntityUid item,
        ItemSlotsComponent? itemSlots = null,
        RMCHardpointSlotsComponent? hardpoints = null)
    {
        item = default;

        if (!TryGetMountedSlot(vehicle, slotId, out var mountedSlot, hardpoints, itemSlots) ||
            mountedSlot.Item is not { } mountedItem)
        {
            return false;
        }

        item = mountedItem;
        return true;
    }

    public bool TryGetMountedSlotHardpointType(
        EntityUid vehicle,
        string slotId,
        out string hardpointType,
        RMCHardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null)
    {
        hardpointType = string.Empty;

        if (!TryGetMountedSlot(vehicle, slotId, out var mountedSlot, hardpoints, itemSlots))
            return false;

        hardpointType = mountedSlot.HardpointType;
        return true;
    }

    public bool TryGetPrimaryTurret(
        EntityUid vehicle,
        out EntityUid turretUid,
        RMCHardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null)
    {
        turretUid = default;

        if (!Resolve(vehicle, ref hardpoints, ref itemSlots, logMissing: false))
            return false;

        foreach (var slot in hardpoints.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id) ||
                !_itemSlots.TryGetSlot(vehicle, slot.Id, out var itemSlot, itemSlots) ||
                !itemSlot.HasItem ||
                itemSlot.Item is not { } item)
            {
                continue;
            }

            if (!HasComp<VehicleTurretComponent>(item) || HasComp<VehicleTurretAttachmentComponent>(item))
                continue;

            turretUid = item;
            return true;
        }

        return false;
    }

    private bool TryGetContainerAncestor<TComponent>(EntityUid uid, out EntityUid ancestor, bool includeSelf = false)
        where TComponent : IComponent
    {
        ancestor = default;
        var query = GetEntityQuery<TComponent>();

        if (includeSelf && query.HasComp(uid))
        {
            ancestor = uid;
            return true;
        }

        var current = uid;
        while (_containers.TryGetContainingContainer((current, null), out var container))
        {
            var owner = container.Owner;
            if (query.HasComp(owner))
            {
                ancestor = owner;
                return true;
            }

            current = owner;
        }

        return false;
    }

    private void EnumerateMountedSlots(
        EntityUid vehicle,
        EntityUid slotOwner,
        RMCHardpointSlotsComponent hardpoints,
        ItemSlotsComponent itemSlots,
        List<RMCVehicleMountedSlot> result,
        string? parentCompositeId,
        EntityUid? parentItem)
    {
        foreach (var slot in hardpoints.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            EntityUid? item = null;
            if (_itemSlots.TryGetSlot(slotOwner, slot.Id, out var itemSlot, itemSlots) &&
                itemSlot.HasItem)
            {
                item = itemSlot.Item;
            }

            var compositeId = parentCompositeId == null
                ? slot.Id
                : RMCVehicleTurretSlotIds.Compose(parentCompositeId, slot.Id);

            result.Add(new RMCVehicleMountedSlot(
                vehicle,
                slotOwner,
                slot.Id,
                compositeId,
                slot.HardpointType,
                item,
                parentItem,
                parentCompositeId));

            if (item is not { } nestedItem ||
                !TryComp(nestedItem, out RMCHardpointSlotsComponent? nestedHardpoints) ||
                !TryComp(nestedItem, out ItemSlotsComponent? nestedItemSlots))
            {
                continue;
            }

            EnumerateMountedSlots(
                vehicle,
                nestedItem,
                nestedHardpoints,
                nestedItemSlots,
                result,
                compositeId,
                nestedItem);
        }
    }

    private void PopulateMountedSlotIds(
        EntityUid slotOwner,
        RMCHardpointSlotsComponent hardpoints,
        ItemSlotsComponent itemSlots,
        HashSet<string> result,
        string? parentCompositeId)
    {
        foreach (var slot in hardpoints.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            var compositeId = parentCompositeId == null
                ? slot.Id
                : RMCVehicleTurretSlotIds.Compose(parentCompositeId, slot.Id);

            result.Add(compositeId);

            if (!_itemSlots.TryGetSlot(slotOwner, slot.Id, out var itemSlot, itemSlots) ||
                !itemSlot.HasItem ||
                itemSlot.Item is not { } nestedItem ||
                !TryComp(nestedItem, out RMCHardpointSlotsComponent? nestedHardpoints) ||
                !TryComp(nestedItem, out ItemSlotsComponent? nestedItemSlots))
            {
                continue;
            }

            PopulateMountedSlotIds(nestedItem, nestedHardpoints, nestedItemSlots, result, compositeId);
        }
    }

    private bool TryGetMountedSlotRecursive(
        EntityUid vehicle,
        EntityUid slotOwner,
        string targetSlotId,
        RMCHardpointSlotsComponent hardpoints,
        ItemSlotsComponent itemSlots,
        string? parentCompositeId,
        EntityUid? parentItem,
        out RMCVehicleMountedSlot mountedSlot)
    {
        foreach (var slot in hardpoints.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            EntityUid? item = null;
            if (_itemSlots.TryGetSlot(slotOwner, slot.Id, out var itemSlot, itemSlots) &&
                itemSlot.HasItem)
            {
                item = itemSlot.Item;
            }

            var compositeId = parentCompositeId == null
                ? slot.Id
                : RMCVehicleTurretSlotIds.Compose(parentCompositeId, slot.Id);

            var current = new RMCVehicleMountedSlot(
                vehicle,
                slotOwner,
                slot.Id,
                compositeId,
                slot.HardpointType,
                item,
                parentItem,
                parentCompositeId);

            if (compositeId == targetSlotId)
            {
                mountedSlot = current;
                return true;
            }

            if (item is not { } nestedItem ||
                !TryComp(nestedItem, out RMCHardpointSlotsComponent? nestedHardpoints) ||
                !TryComp(nestedItem, out ItemSlotsComponent? nestedItemSlots))
            {
                continue;
            }

            if (TryGetMountedSlotRecursive(
                    vehicle,
                    nestedItem,
                    targetSlotId,
                    nestedHardpoints,
                    nestedItemSlots,
                    compositeId,
                    nestedItem,
                    out mountedSlot))
            {
                return true;
            }
        }

        mountedSlot = default;
        return false;
    }

    private bool TryGetMountedSlotByItemRecursive(
        EntityUid vehicle,
        EntityUid slotOwner,
        EntityUid targetItem,
        RMCHardpointSlotsComponent hardpoints,
        ItemSlotsComponent itemSlots,
        string? parentCompositeId,
        EntityUid? parentItem,
        out RMCVehicleMountedSlot mountedSlot)
    {
        foreach (var slot in hardpoints.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            EntityUid? item = null;
            if (_itemSlots.TryGetSlot(slotOwner, slot.Id, out var itemSlot, itemSlots) &&
                itemSlot.HasItem)
            {
                item = itemSlot.Item;
            }

            var compositeId = parentCompositeId == null
                ? slot.Id
                : RMCVehicleTurretSlotIds.Compose(parentCompositeId, slot.Id);

            var current = new RMCVehicleMountedSlot(
                vehicle,
                slotOwner,
                slot.Id,
                compositeId,
                slot.HardpointType,
                item,
                parentItem,
                parentCompositeId);

            if (item == targetItem)
            {
                mountedSlot = current;
                return true;
            }

            if (item is not { } nestedItem ||
                !TryComp(nestedItem, out RMCHardpointSlotsComponent? nestedHardpoints) ||
                !TryComp(nestedItem, out ItemSlotsComponent? nestedItemSlots))
            {
                continue;
            }

            if (TryGetMountedSlotByItemRecursive(
                    vehicle,
                    nestedItem,
                    targetItem,
                    nestedHardpoints,
                    nestedItemSlots,
                    compositeId,
                    nestedItem,
                    out mountedSlot))
            {
                return true;
            }
        }

        mountedSlot = default;
        return false;
    }
}
