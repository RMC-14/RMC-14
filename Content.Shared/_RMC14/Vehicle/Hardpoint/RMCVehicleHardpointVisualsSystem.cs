using System;
using System.Collections.Generic;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
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
        SubscribeLocalEvent<RMCVehicleHardpointVisualsComponent, ComponentGetState>(OnGetState);
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

    private void OnGetState(Entity<RMCVehicleHardpointVisualsComponent> ent, ref ComponentGetState args)
    {
        if (_net.IsClient)
            return;

        var layers = new List<RMCVehicleHardpointLayerState>(ent.Comp.Layers);
        args.State = new RMCVehicleHardpointVisualsComponentState(layers);
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
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            var layer = slot.VisualLayer;
            var layerKey = layer.ToLowerInvariant();
            var state = string.Empty;
            var usesOverlay = false;
            if (!_itemSlots.TryGetSlot(vehicle, slot.Id, out var itemSlot, itemSlots))
                continue;

            if (itemSlot.HasItem)
            {
                var item = itemSlot.Item!.Value;
                state = ResolveVisualState(item, out usesOverlay);
            }

            if (string.IsNullOrWhiteSpace(layer))
            {
                continue;
            }

            if (usesOverlay)
                state = string.Empty;

            layerKey = layer.ToLowerInvariant();
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
            {
                return;
            }
        }

        visuals.Layers = newLayers;
        Dirty(vehicle, visuals);
    }

    private string ResolveVisualState(EntityUid item, out bool usesOverlay, int depth = 0)
    {
        usesOverlay = false;
        if (depth > 2)
            return string.Empty;

        if (TryComp(item, out VehicleTurretComponent? turret) && turret.ShowOverlay)
            usesOverlay = true;

        if (TryComp(item, out RMCHardpointSlotsComponent? attachedSlots) &&
            TryComp(item, out ItemSlotsComponent? attachedItemSlots))
        {
            foreach (var slot in attachedSlots.Slots)
            {
                if (string.IsNullOrWhiteSpace(slot.Id))
                    continue;

                if (!_itemSlots.TryGetSlot(item, slot.Id, out var itemSlot, attachedItemSlots) || !itemSlot.HasItem)
                    continue;

                var child = itemSlot.Item!.Value;
                var childState = ResolveVisualState(child, out var childOverlay, depth + 1);
                usesOverlay |= childOverlay;
                if (!string.IsNullOrWhiteSpace(childState))
                    return childState;
            }
        }

        if (TryComp(item, out RMCHardpointVisualComponent? visual) &&
            !string.IsNullOrWhiteSpace(visual.VehicleState))
        {
            return visual.VehicleState;
        }

        if (TryComp(item, out VehicleTurretComponent? turretOverlay) &&
            !string.IsNullOrWhiteSpace(turretOverlay.OverlayState))
        {
            return turretOverlay.OverlayState;
        }

        return string.Empty;
    }
}
