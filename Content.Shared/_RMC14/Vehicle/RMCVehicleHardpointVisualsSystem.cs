using System;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCVehicleHardpointVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCVehicleHardpointVisualsComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RMCVehicleHardpointVisualsComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<RMCHardpointSlotsChangedEvent>(OnHardpointSlotsChanged);
    }

    private void OnInit(Entity<RMCVehicleHardpointVisualsComponent> ent, ref ComponentInit args)
    {
        if (_net.IsClient)
            return;

        UpdateAppearance(ent.Owner);
    }

    private void OnInit(Entity<RMCVehicleHardpointVisualsComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        UpdateAppearance(ent.Owner);
    }

    private void OnHardpointSlotsChanged(RMCHardpointSlotsChangedEvent args)
    {
        if (_net.IsClient)
            return;

        if (!HasComp<RMCVehicleHardpointVisualsComponent>(args.Vehicle))
            return;

        UpdateAppearance(args.Vehicle);
    }

    private void UpdateAppearance(
        EntityUid vehicle,
        RMCHardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null,
        AppearanceComponent? appearance = null)
    {
        if (!Resolve(vehicle, ref hardpoints, ref itemSlots, ref appearance, logMissing: false))
            return;

        var primary = string.Empty;
        var secondary = string.Empty;
        var support = string.Empty;

        foreach (var slot in hardpoints.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            if (!_itemSlots.TryGetSlot(vehicle, slot.Id, out var itemSlot, itemSlots) || !itemSlot.HasItem)
                continue;

            var item = itemSlot.Item!.Value;
            if (!TryComp(item, out RMCHardpointVisualComponent? visual))
                continue;

            var state = visual.VehicleState;
            if (string.IsNullOrWhiteSpace(state))
                continue;

            if (slot.HardpointType.Equals("Primary", StringComparison.OrdinalIgnoreCase))
                primary = state;
            else if (slot.HardpointType.Equals("Secondary", StringComparison.OrdinalIgnoreCase))
                secondary = state;
            else if (slot.HardpointType.Equals("Support", StringComparison.OrdinalIgnoreCase))
                support = state;
        }

        _appearance.SetData(vehicle, RMCVehicleHardpointVisuals.PrimaryState, primary, appearance);
        _appearance.SetData(vehicle, RMCVehicleHardpointVisuals.SecondaryState, secondary, appearance);
        _appearance.SetData(vehicle, RMCVehicleHardpointVisuals.SupportState, support, appearance);
    }
}
