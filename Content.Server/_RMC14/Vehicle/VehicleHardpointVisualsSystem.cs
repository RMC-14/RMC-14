using System.Collections.Generic;
using Content.Shared._RMC14.Vehicle;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;

namespace Content.Server._RMC14.Vehicle;

public sealed class VehicleHardpointVisualsSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleHardpointVisualsComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<VehicleHardpointVisualsComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<VehicleHardpointVisualsComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<HardpointSlotsChangedEvent>(OnHardpointSlotsChanged);
    }

    private void OnInit(Entity<VehicleHardpointVisualsComponent> ent, ref ComponentInit args)
    {
        UpdateAppearance(ent.Owner);
    }

    private void OnInit(Entity<VehicleHardpointVisualsComponent> ent, ref MapInitEvent args)
    {
        UpdateAppearance(ent.Owner);
    }

    private void OnHardpointSlotsChanged(HardpointSlotsChangedEvent args)
    {
        if (!HasComp<VehicleHardpointVisualsComponent>(args.Vehicle))
            return;

        UpdateAppearance(args.Vehicle);
    }

    private void OnGetState(Entity<VehicleHardpointVisualsComponent> ent, ref ComponentGetState args)
    {
        var layers = new List<VehicleHardpointLayerState>(ent.Comp.Layers);
        args.State = new VehicleHardpointVisualsComponentState(layers);
    }

    private void UpdateAppearance(
        EntityUid vehicle,
        HardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null,
        VehicleHardpointVisualsComponent? visuals = null)
    {
        if (!Resolve(vehicle, ref hardpoints, ref itemSlots, ref visuals, logMissing: false))
            return;

        var newLayers = new List<VehicleHardpointLayerState>(hardpoints.Slots.Count);
        var indexByLayer = new Dictionary<string, int>();

        foreach (var slot in hardpoints.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            var layer = slot.VisualLayer;
            if (string.IsNullOrWhiteSpace(layer))
                continue;

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

            if (usesOverlay)
                state = string.Empty;

            if (indexByLayer.TryGetValue(layerKey, out var existingIndex))
            {
                if (!string.IsNullOrWhiteSpace(state))
                    newLayers[existingIndex] = new VehicleHardpointLayerState(layer, state);
                continue;
            }

            indexByLayer[layerKey] = newLayers.Count;
            newLayers.Add(new VehicleHardpointLayerState(layer, state));
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

    private string ResolveVisualState(EntityUid item, out bool usesOverlay, int depth = 0)
    {
        usesOverlay = false;
        if (depth > 2)
            return string.Empty;

        if (TryComp(item, out VehicleTurretComponent? turret) && turret.ShowOverlay)
            usesOverlay = true;

        if (TryComp(item, out HardpointSlotsComponent? attachedSlots) &&
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

        if (TryComp(item, out HardpointVisualComponent? visual) &&
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
