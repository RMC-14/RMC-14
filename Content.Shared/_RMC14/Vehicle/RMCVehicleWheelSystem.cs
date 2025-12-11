using System;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCVehicleWheelSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly VehicleSystem _vehicles = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCVehicleWheelSlotsComponent, ComponentInit>(OnWheelInit);
        SubscribeLocalEvent<RMCVehicleWheelSlotsComponent, MapInitEvent>(OnWheelMapInit);
        SubscribeLocalEvent<RMCVehicleWheelSlotsComponent, EntInsertedIntoContainerMessage>(OnWheelInserted);
        SubscribeLocalEvent<RMCVehicleWheelSlotsComponent, EntRemovedFromContainerMessage>(OnWheelRemoved);
        SubscribeLocalEvent<RMCVehicleWheelSlotsComponent, VehicleCanRunEvent>(OnVehicleCanRun);
    }

    private void OnWheelInit(Entity<RMCVehicleWheelSlotsComponent> ent, ref ComponentInit args)
    {
        EnsureSlots(ent.Owner, ent.Comp);
        UpdateAppearance(ent.Owner, ent.Comp);
    }

    private void OnWheelMapInit(Entity<RMCVehicleWheelSlotsComponent> ent, ref MapInitEvent args)
    {
        EnsureSlots(ent.Owner, ent.Comp);
        UpdateAppearance(ent.Owner, ent.Comp);
    }

    private void OnWheelInserted(Entity<RMCVehicleWheelSlotsComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!IsWheelSlot(ent.Comp, args.Container.ID))
            return;

        UpdateAppearance(ent.Owner, ent.Comp);
        RefreshCanRun(ent.Owner);
    }

    private void OnWheelRemoved(Entity<RMCVehicleWheelSlotsComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!IsWheelSlot(ent.Comp, args.Container.ID))
            return;

        UpdateAppearance(ent.Owner, ent.Comp);
        RefreshCanRun(ent.Owner);
    }

    private void OnVehicleCanRun(Entity<RMCVehicleWheelSlotsComponent> ent, ref VehicleCanRunEvent args)
    {
        if (!args.CanRun)
            return;

        if (!HasAllWheels(ent.Owner, ent.Comp))
            args.CanRun = false;
    }

    private void EnsureSlots(EntityUid uid, RMCVehicleWheelSlotsComponent component, ItemSlotsComponent? itemSlots = null)
    {
        itemSlots ??= EnsureComp<ItemSlotsComponent>(uid);

        if (component.Slots.Count == 0 && TryComp<RMCHardpointSlotsComponent>(uid, out var hardpoints))
        {
            foreach (var slot in hardpoints.Slots)
            {
                if (string.Equals(slot.HardpointType, RMCVehicleWheelSlotsComponent.HardpointTypeId, StringComparison.OrdinalIgnoreCase))
                    component.Slots.Add(slot.Id);
            }
        }

        if (component.Slots.Count == 0)
        {
            for (var i = 0; i < component.SlotCount; i++)
            {
                component.Slots.Add($"{component.SlotPrefix}-{i + 1}");
            }
        }

        if (component.WheelWhitelist.Components == null || component.WheelWhitelist.Components.Length == 0)
            component.WheelWhitelist.Components = new[] { RMCVehicleWheelSlotsComponent.WheelComponentId };

        foreach (var slotId in component.Slots)
        {
            if (_itemSlots.TryGetSlot(uid, slotId, out _, itemSlots))
                continue;

            var slot = new ItemSlot
            {
                Whitelist = component.WheelWhitelist,
                InsertOnInteract = true,
                EjectOnInteract = true,
                EjectOnUse = false,
                Swap = true,
            };

            _itemSlots.AddItemSlot(uid, slotId, slot, itemSlots);
        }
    }

    private bool IsWheelSlot(RMCVehicleWheelSlotsComponent component, string? id)
    {
        return id != null && component.Slots.Contains(id);
    }

    private bool HasAllWheels(EntityUid uid, RMCVehicleWheelSlotsComponent? component = null, ItemSlotsComponent? itemSlots = null)
    {
        if (!Resolve(uid, ref component, false) || !Resolve(uid, ref itemSlots, false))
            return false;

        if (component.Slots.Count == 0)
            return false;

        foreach (var slotId in component.Slots)
        {
            if (!_itemSlots.TryGetSlot(uid, slotId, out var slot, itemSlots) || !slot.HasItem)
                return false;
        }

        return true;
    }

    private int GetWheelCount(EntityUid uid, RMCVehicleWheelSlotsComponent component, ItemSlotsComponent? itemSlots = null)
    {
        var count = 0;

        if (!Resolve(uid, ref itemSlots, false))
            return count;

        foreach (var slotId in component.Slots)
        {
            if (_itemSlots.TryGetSlot(uid, slotId, out var slot, itemSlots) && slot.HasItem)
                count++;
        }

        return count;
    }

    private void UpdateAppearance(EntityUid uid, RMCVehicleWheelSlotsComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        var hasAll = HasAllWheels(uid, component);
        _appearance.SetData(uid, RMCVehicleWheelVisuals.HasAllWheels, hasAll, appearance);

        var count = GetWheelCount(uid, component);
        _appearance.SetData(uid, RMCVehicleWheelVisuals.WheelCount, count, appearance);
    }

    private void RefreshCanRun(EntityUid uid)
    {
        if (!TryComp<VehicleComponent>(uid, out var vehicle))
            return;

        _vehicles.RefreshCanRun((uid, vehicle));
    }
}
