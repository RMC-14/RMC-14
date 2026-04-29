using System.Diagnostics.CodeAnalysis;
using Content.Shared.Containers.ItemSlots;

namespace Content.Shared._RMC14.Vehicle;

internal readonly record struct HardpointSlotLocation(
    EntityUid Owner,
    HardpointSlotsComponent Slots,
    HardpointStateComponent State,
    ItemSlotsComponent ItemSlots,
    HardpointSlot Definition,
    ItemSlot Slot);

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
        location = default;

        if (string.IsNullOrWhiteSpace(slotId))
            return false;

        if (VehicleTurretSlotIds.TryParse(slotId, out var parentSlotId, out var childSlotId))
        {
            if (!TryResolveSlotLocation(owner, hardpoints, parentSlotId, out var parentLocation))
                return false;

            if (parentLocation.Slot.Item is not { } attached)
                return false;

            if (!TryComp(attached, out HardpointSlotsComponent? childSlots) ||
                !TryComp(attached, out HardpointStateComponent? childState) ||
                !TryComp(attached, out ItemSlotsComponent? childItemSlots))
            {
                return false;
            }

            return TryResolveSlotLocation(attached, childSlots, childSlotId, out location);
        }

        if (!TryGetSlot(hardpoints, slotId, out var slot))
            return false;

        if (!TryComp(owner, out HardpointStateComponent? state) ||
            !TryComp(owner, out ItemSlotsComponent? itemSlots) ||
            !_itemSlots.TryGetSlot(owner, slot.Id, out var itemSlot, itemSlots))
        {
            return false;
        }

        location = new HardpointSlotLocation(owner, hardpoints, state, itemSlots, slot, itemSlot);
        return true;
    }

    internal bool TryFindEmptyInstallLocation(
        EntityUid owner,
        HardpointSlotsComponent hardpoints,
        EntityUid item,
        [NotNullWhen(true)] out HardpointSlotLocation location)
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

            location = new HardpointSlotLocation(owner, hardpoints, state, itemSlots, slot, itemSlot);
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

            if (TryFindEmptyInstallLocation(installed, childSlots, item, out location))
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
