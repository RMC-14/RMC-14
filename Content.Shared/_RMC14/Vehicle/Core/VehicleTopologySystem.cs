using System;
using System.Collections.Generic;
using Content.Shared.Containers.ItemSlots;
using Content.Shared._RMC14.Weapons.Ranged.Ammo.BulletBox;
using Content.Shared.Vehicle.Components;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

[Serializable, NetSerializable]
public readonly record struct VehicleSlotPath(string Root, string? Child = null)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(Root);
    public bool IsNested => !string.IsNullOrWhiteSpace(Child);

    public string ToCompositeId()
    {
        return IsNested
            ? VehicleTurretSlotIds.Compose(Root, Child!)
            : Root;
    }

    public VehicleSlotPath Append(string child)
    {
        return IsNested
            ? new VehicleSlotPath(Root, VehicleTurretSlotIds.Compose(Child!, child))
            : new VehicleSlotPath(Root, child);
    }

    public static bool TryParse(string? value, out VehicleSlotPath path)
    {
        path = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (VehicleTurretSlotIds.TryParse(value, out var parent, out var child))
        {
            path = new VehicleSlotPath(parent, child);
            return true;
        }

        path = new VehicleSlotPath(value);
        return true;
    }
}

public readonly record struct VehicleMountedSlot(
    EntityUid Vehicle,
    EntityUid SlotOwner,
    string SlotId,
    VehicleSlotPath Path,
    string HardpointType,
    EntityUid? Item,
    EntityUid? ParentItem,
    VehicleSlotPath? ParentPath)
{
    public bool HasItem => Item != null;
    public bool IsNested => ParentItem != null;
    public string CompositeId => Path.ToCompositeId();
    public string? ParentSlotId => ParentPath?.ToCompositeId();
}

public readonly record struct VehicleMountedAmmoProvider(
    VehicleMountedSlot Slot,
    EntityUid AmmoUid,
    BallisticAmmoProviderComponent Ammo,
    VehicleHardpointAmmoComponent HardpointAmmo,
    RefillableByBulletBoxComponent Refill);

public sealed class VehicleTopologySystem : EntitySystem
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

    public List<VehicleMountedSlot> GetMountedSlots(
        EntityUid vehicle,
        HardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null)
    {
        var result = new List<VehicleMountedSlot>();

        if (!Resolve(vehicle, ref hardpoints, ref itemSlots, logMissing: false))
            return result;

        EnumerateMountedSlots(
            vehicle,
            vehicle,
            hardpoints,
            itemSlots,
            result,
            parentPath: null,
            parentItem: null);

        return result;
    }

    public HashSet<string> GetMountedSlotIds(
        EntityUid vehicle,
        HardpointSlotsComponent? hardpoints = null,
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
        out VehicleMountedSlot mountedSlot,
        HardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null)
    {
        mountedSlot = default;

        if (!VehicleSlotPath.TryParse(slotId, out var path))
            return false;

        return TryGetMountedSlot(vehicle, path, out mountedSlot, hardpoints, itemSlots);
    }

    public bool TryGetMountedSlot(
        EntityUid vehicle,
        VehicleSlotPath path,
        out VehicleMountedSlot mountedSlot,
        HardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null)
    {
        mountedSlot = default;

        if (!path.IsValid || !Resolve(vehicle, ref hardpoints, ref itemSlots, logMissing: false))
            return false;

        return TryGetMountedSlotRecursive(
            vehicle,
            vehicle,
            path,
            hardpoints,
            itemSlots,
            parentPath: null,
            parentItem: null,
            out mountedSlot);
    }

    public bool TryGetMountedSlotByItem(
        EntityUid vehicle,
        EntityUid item,
        out VehicleMountedSlot mountedSlot,
        HardpointSlotsComponent? hardpoints = null,
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
            parentPath: null,
            parentItem: null,
            out mountedSlot);
    }

    public bool TryGetMountedSlotItem(
        EntityUid vehicle,
        string slotId,
        out EntityUid item,
        ItemSlotsComponent? itemSlots = null,
        HardpointSlotsComponent? hardpoints = null)
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
        HardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null)
    {
        hardpointType = string.Empty;

        if (!TryGetMountedSlot(vehicle, slotId, out var mountedSlot, hardpoints, itemSlots))
            return false;

        hardpointType = mountedSlot.HardpointType;
        return true;
    }

    public List<VehicleMountedAmmoProvider> GetMountedAmmoProviders(
        EntityUid vehicle,
        HardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null)
    {
        var result = new List<VehicleMountedAmmoProvider>();

        foreach (var slot in GetMountedSlots(vehicle, hardpoints, itemSlots))
        {
            if (slot.Item is not { } item)
                continue;

            if (!TryGetAmmoProviderFromItem(slot, item, out var provider))
                continue;

            result.Add(provider);
        }

        return result;
    }

    public bool TryGetMountedAmmoProvider(
        EntityUid vehicle,
        string? slotId,
        out VehicleMountedAmmoProvider provider,
        HardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null)
    {
        provider = default;

        if (!VehicleSlotPath.TryParse(slotId, out var path))
            return false;

        return TryGetMountedAmmoProvider(vehicle, path, out provider, hardpoints, itemSlots);
    }

    public bool TryGetMountedAmmoProvider(
        EntityUid vehicle,
        VehicleSlotPath path,
        out VehicleMountedAmmoProvider provider,
        HardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null)
    {
        provider = default;

        if (!path.IsValid ||
            !TryGetMountedSlot(vehicle, path, out var mountedSlot, hardpoints, itemSlots) ||
            mountedSlot.Item is not { } item)
        {
            return false;
        }

        return TryGetAmmoProviderFromItem(mountedSlot, item, out provider);
    }

    public bool TryGetPrimaryTurret(
        EntityUid vehicle,
        out EntityUid turretUid,
        HardpointSlotsComponent? hardpoints = null,
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

    private bool TryGetAmmoProviderFromItem(
        VehicleMountedSlot slot,
        EntityUid item,
        out VehicleMountedAmmoProvider provider)
    {
        provider = default;

        if (!TryComp(item, out BallisticAmmoProviderComponent? ammo))
            return false;

        if (!TryComp(item, out VehicleHardpointAmmoComponent? hardpointAmmo))
            return false;

        if (!TryComp(item, out RefillableByBulletBoxComponent? refill))
            return false;

        provider = new VehicleMountedAmmoProvider(slot, item, ammo, hardpointAmmo, refill);
        return true;
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
        HardpointSlotsComponent hardpoints,
        ItemSlotsComponent itemSlots,
        List<VehicleMountedSlot> result,
        VehicleSlotPath? parentPath,
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

            var path = parentPath?.Append(slot.Id) ?? new VehicleSlotPath(slot.Id);

            result.Add(new VehicleMountedSlot(
                vehicle,
                slotOwner,
                slot.Id,
                path,
                slot.HardpointType,
                item,
                parentItem,
                parentPath));

            if (item is not { } nestedItem ||
                !TryComp(nestedItem, out HardpointSlotsComponent? nestedHardpoints) ||
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
                path,
                nestedItem);
        }
    }

    private void PopulateMountedSlotIds(
        EntityUid slotOwner,
        HardpointSlotsComponent hardpoints,
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
                : VehicleTurretSlotIds.Compose(parentCompositeId, slot.Id);

            result.Add(compositeId);

            if (!_itemSlots.TryGetSlot(slotOwner, slot.Id, out var itemSlot, itemSlots) ||
                !itemSlot.HasItem ||
                itemSlot.Item is not { } nestedItem ||
                !TryComp(nestedItem, out HardpointSlotsComponent? nestedHardpoints) ||
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
        VehicleSlotPath targetPath,
        HardpointSlotsComponent hardpoints,
        ItemSlotsComponent itemSlots,
        VehicleSlotPath? parentPath,
        EntityUid? parentItem,
        out VehicleMountedSlot mountedSlot)
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

            var path = parentPath?.Append(slot.Id) ?? new VehicleSlotPath(slot.Id);

            var current = new VehicleMountedSlot(
                vehicle,
                slotOwner,
                slot.Id,
                path,
                slot.HardpointType,
                item,
                parentItem,
                parentPath);

            if (PathEquals(path, targetPath))
            {
                mountedSlot = current;
                return true;
            }

            if (item is not { } nestedItem ||
                !TryComp(nestedItem, out HardpointSlotsComponent? nestedHardpoints) ||
                !TryComp(nestedItem, out ItemSlotsComponent? nestedItemSlots))
            {
                continue;
            }

            if (TryGetMountedSlotRecursive(
                    vehicle,
                    nestedItem,
                    targetPath,
                    nestedHardpoints,
                    nestedItemSlots,
                    path,
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
        HardpointSlotsComponent hardpoints,
        ItemSlotsComponent itemSlots,
        VehicleSlotPath? parentPath,
        EntityUid? parentItem,
        out VehicleMountedSlot mountedSlot)
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

            var path = parentPath?.Append(slot.Id) ?? new VehicleSlotPath(slot.Id);

            var current = new VehicleMountedSlot(
                vehicle,
                slotOwner,
                slot.Id,
                path,
                slot.HardpointType,
                item,
                parentItem,
                parentPath);

            if (item == targetItem)
            {
                mountedSlot = current;
                return true;
            }

            if (item is not { } nestedItem ||
                !TryComp(nestedItem, out HardpointSlotsComponent? nestedHardpoints) ||
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
                    path,
                    nestedItem,
                    out mountedSlot))
            {
                return true;
            }
        }

        mountedSlot = default;
        return false;
    }

    private static bool PathEquals(VehicleSlotPath left, VehicleSlotPath right)
    {
        return string.Equals(left.Root, right.Root, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.Child, right.Child, StringComparison.OrdinalIgnoreCase);
    }
}
