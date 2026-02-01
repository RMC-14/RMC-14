using System;
using System.Collections.Generic;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCVehicleHardpointVisualsSystem : EntitySystem
{
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
        RMCVehicleHardpointVisualsComponent? visuals = null)
    {
        if (!Resolve(vehicle, ref hardpoints, ref itemSlots, ref visuals, logMissing: false))
            return;

        var newLayers = new List<RMCVehicleHardpointLayerState>(hardpoints.Slots.Count);
        var indexByLayer = new Dictionary<string, int>();

        foreach (var slot in hardpoints.Slots)
        {
            var layer = slot.VisualLayer;
            if (string.IsNullOrWhiteSpace(slot.Id) || string.IsNullOrWhiteSpace(layer))
                continue;

            var layerKey = layer.ToLowerInvariant();
            var state = string.Empty;
            if (_itemSlots.TryGetSlot(vehicle, slot.Id, out var itemSlot, itemSlots) && itemSlot.HasItem)
            {
                var item = itemSlot.Item!.Value;
                if (TryComp(item, out RMCHardpointVisualComponent? visual) &&
                    !string.IsNullOrWhiteSpace(visual.VehicleState))
                {
                    state = visual.VehicleState;
                }
            }

            if (indexByLayer.TryGetValue(layerKey, out var existingIndex))
            {
                if (!string.IsNullOrWhiteSpace(state))
                    newLayers[existingIndex] = new RMCVehicleHardpointLayerState(layer, state);
                continue;
            }

            indexByLayer[layerKey] = newLayers.Count;
            newLayers.Add(new RMCVehicleHardpointLayerState(layer, state));
        }

        if (visuals.Layers.Count == newLayers.Count)
        {
            var unchanged = true;
            for (var i = 0; i < visuals.Layers.Count; i++)
            {
                if (!visuals.Layers[i].Equals(newLayers[i]))
                {
                    unchanged = false;
                    break;
                }
            }

            if (unchanged)
                return;
        }

        visuals.Layers = newLayers;
        Dirty(vehicle, visuals);
    }
}
