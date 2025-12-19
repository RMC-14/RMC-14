using System;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Content.Shared._RMC14.Repairable;
using Content.Shared._RMC14.Vehicle;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Localization;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCVehicleWheelSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly VehicleSystem _vehicles = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCRepairableSystem _repairable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly RMCHardpointSystem _hardpoints = default!;

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

            if (slot.Item is not { } wheel || !IsWheelFunctional(wheel))
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
            if (_itemSlots.TryGetSlot(uid, slotId, out var slot, itemSlots) &&
                slot.HasItem)
            {
                count++;
            }
        }

        return count;
    }

    private int GetFunctionalWheelCount(EntityUid uid, RMCVehicleWheelSlotsComponent component, ItemSlotsComponent? itemSlots = null)
    {
        var count = 0;

        if (!Resolve(uid, ref itemSlots, false))
            return count;

        foreach (var slotId in component.Slots)
        {
            if (_itemSlots.TryGetSlot(uid, slotId, out var slot, itemSlots) &&
                slot.HasItem &&
                slot.Item is { } wheel &&
                IsWheelFunctional(wheel))
            {
                count++;
            }
        }

        return count;
    }

    // For some reason I thought each wheels was its own thing, so did this. but at least it supports it if that ever is a thing
    private float GetAverageWheelIntegrityFraction(EntityUid uid, RMCVehicleWheelSlotsComponent component, ItemSlotsComponent? itemSlots = null)
    {
        if (!Resolve(uid, ref itemSlots, false))
            return 1f;

        var total = 0f;
        var installed = 0;

        foreach (var slotId in component.Slots)
        {
            if (!_itemSlots.TryGetSlot(uid, slotId, out var slot, itemSlots) || !slot.HasItem)
                continue;

            installed++;

            var fraction = 1f;
            if (slot.Item is { } wheel && TryComp(wheel, out RMCHardpointIntegrityComponent? integrity))
            {
                var max = integrity.MaxIntegrity > 0f ? integrity.MaxIntegrity : 1f;
                fraction = Math.Clamp(integrity.Integrity / max, 0f, 1f);
            }

            total += fraction;
        }

        if (installed == 0)
            return 1f;

        return Math.Clamp(total / installed, 0f, 1f);
    }

    private void UpdateAppearance(EntityUid uid, RMCVehicleWheelSlotsComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        var hasAll = HasAllWheels(uid, component);
        _appearance.SetData(uid, RMCVehicleWheelVisuals.HasAllWheels, hasAll, appearance);

        var count = GetWheelCount(uid, component);
        _appearance.SetData(uid, RMCVehicleWheelVisuals.WheelCount, count, appearance);

        var functional = GetFunctionalWheelCount(uid, component);
        _appearance.SetData(uid, RMCVehicleWheelVisuals.WheelFunctionalCount, functional, appearance);

        var averageIntegrity = GetAverageWheelIntegrityFraction(uid, component);
        _appearance.SetData(uid, RMCVehicleWheelVisuals.WheelIntegrityFraction, averageIntegrity, appearance);
    }

    private bool IsWheelFunctional(EntityUid wheel, RMCHardpointIntegrityComponent? integrity = null)
    {
        if (!Resolve(wheel, ref integrity, logMissing: false))
            return true;

        return integrity.Integrity > 0f;
    }

    public void DamageWheels(EntityUid vehicle, float amount)
    {
        if (amount <= 0f || !TryComp(vehicle, out RMCVehicleWheelSlotsComponent? wheels))
            return;

        if (!TryComp(vehicle, out ItemSlotsComponent? itemSlots))
            return;

        var changed = false;

        foreach (var slotId in wheels.Slots)
        {
            if (!_itemSlots.TryGetSlot(vehicle, slotId, out var slot, itemSlots) ||
                slot.Item is not { } wheel)
                continue;

            if (_hardpoints.DamageHardpoint(vehicle, wheel, amount))
                changed = true;
        }

        if (!changed)
            return;

        UpdateAppearance(vehicle, wheels);
        RefreshCanRun(vehicle);
    }

    public void OnWheelDamaged(EntityUid vehicle)
    {
        if (!TryComp(vehicle, out RMCVehicleWheelSlotsComponent? wheels))
            return;

        UpdateAppearance(vehicle, wheels);
        RefreshCanRun(vehicle);
    }

    private void RefreshCanRun(EntityUid uid)
    {
        if (!TryComp<VehicleComponent>(uid, out var vehicle))
            return;

        _vehicles.RefreshCanRun((uid, vehicle));
    }
}
