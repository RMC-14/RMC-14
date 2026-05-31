using System.Diagnostics.CodeAnalysis;
using Content.Shared.Containers.ItemSlots;

namespace Content.Shared._RMC14.Vehicle;

internal readonly record struct HardpointSlotLocation(
    EntityUid Owner,
    HardpointSlotsComponent Slots,
    HardpointStateComponent State,
    ItemSlotsComponent ItemSlots,
    HardpointSlot Definition,
    ItemSlot Slot,
    VehicleSlotPath Path);

public sealed partial class HardpointSystem
{
    internal HardpointStateComponent EnsureState(EntityUid uid)
    {
        return EnsureComp<HardpointStateComponent>(uid);
    }

    internal bool TryResolveSlotLocation(
        EntityUid owner,
        HardpointSlotsComponent hardpoints,
        string? slotId,
        [NotNullWhen(true)] out HardpointSlotLocation location)
    {
        if (!VehicleSlotPath.TryParse(slotId, out var path))
        {
            location = default;
            return false;
        }

        return TryResolveSlotLocation(owner, hardpoints, path, out location, parentPath: null);
    }

    private bool TryResolveSlotLocation(
        EntityUid owner,
        HardpointSlotsComponent hardpoints,
        VehicleSlotPath targetPath,
        [NotNullWhen(true)] out HardpointSlotLocation location,
        VehicleSlotPath? parentPath)
    {
        location = default;

        if (!TryComp(owner, out HardpointStateComponent? state) ||
            !TryComp(owner, out ItemSlotsComponent? itemSlots))
        {
            return false;
        }

        foreach (var slot in hardpoints.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            if (!_itemSlots.TryGetSlot(owner, slot.Id, out var itemSlot, itemSlots))
                continue;

            var path = parentPath?.Append(slot.Id) ?? new VehicleSlotPath(slot.Id);

            if (path == targetPath)
            {
                location = new HardpointSlotLocation(owner, hardpoints, state, itemSlots, slot, itemSlot, path);
                return true;
            }

            if (itemSlot.Item is not { } attached ||
                !TryComp(attached, out HardpointSlotsComponent? childSlots))
            {
                continue;
            }

            if (TryResolveSlotLocation(attached, childSlots, targetPath, out location, path))
                return true;
        }

        return false;
    }

    internal bool TryFindEmptyInstallLocation(
        EntityUid owner,
        HardpointSlotsComponent hardpoints,
        EntityUid item,
        [NotNullWhen(true)] out HardpointSlotLocation location)
    {
        return TryFindEmptyInstallLocation(owner, hardpoints, item, out location, parentPath: null);
    }

    private bool TryFindEmptyInstallLocation(
        EntityUid owner,
        HardpointSlotsComponent hardpoints,
        EntityUid item,
        [NotNullWhen(true)] out HardpointSlotLocation location,
        VehicleSlotPath? parentPath)
    {
        location = default;

        if (!TryComp(owner, out HardpointStateComponent? state) ||
            !TryComp(owner, out ItemSlotsComponent? itemSlots))
        {
            return false;
        }

        foreach (var slot in hardpoints.Slots)
        {
            if (!IsValidHardpoint(item, hardpoints, slot))
                continue;

            if (!_itemSlots.TryGetSlot(owner, slot.Id, out var itemSlot, itemSlots) || itemSlot.HasItem)
                continue;

            var path = parentPath?.Append(slot.Id) ?? new VehicleSlotPath(slot.Id);
            location = new HardpointSlotLocation(owner, hardpoints, state, itemSlots, slot, itemSlot, path);
            return true;
        }

        foreach (var slot in hardpoints.Slots)
        {
            if (!_itemSlots.TryGetSlot(owner, slot.Id, out var itemSlot, itemSlots) ||
                itemSlot.Item is not { } installed)
            {
                continue;
            }

            if (!TryComp(installed, out HardpointSlotsComponent? childSlots))
                continue;

            var path = parentPath?.Append(slot.Id) ?? new VehicleSlotPath(slot.Id);
            if (TryFindEmptyInstallLocation(installed, childSlots, item, out location, path))
                return true;
        }

        return false;
    }

    internal bool TryGetInstalledSlotLocation(
        EntityUid owner,
        HardpointSlotsComponent hardpoints,
        string? slotId,
        [NotNullWhen(true)] out HardpointSlotLocation location,
        out EntityUid installed)
    {
        installed = default;

        if (!TryResolveSlotLocation(owner, hardpoints, slotId, out location))
            return false;

        if (location.Slot.Item is not { } item)
            return false;

        installed = item;
        return true;
    }
}
