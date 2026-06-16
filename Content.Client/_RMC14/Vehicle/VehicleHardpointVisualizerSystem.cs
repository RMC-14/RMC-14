using System.Collections.Generic;
using Content.Shared._RMC14.Vehicle;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client._RMC14.Vehicle;

public sealed class VehicleHardpointVisualizerSystem : VisualizerSystem<VehicleHardpointVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, VehicleHardpointVisualsComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;
        if (sprite == null)
            return;

        if (!AppearanceSystem.TryGetData(uid, VehicleHardpointVisualsVisuals.Layers, out List<VehicleHardpointLayerState>? layers, args.Component) ||
            layers == null)
        {
            return;
        }

        foreach (var entry in layers)
        {
            UpdateLayer(sprite, entry.Layer, entry.State);
        }
    }

    private void UpdateLayer(SpriteComponent sprite, string layerMap, string state)
    {
        if (!sprite.LayerMapTryGet(layerMap, out var layer))
            return;

        if (string.IsNullOrWhiteSpace(state))
        {
            sprite.LayerSetVisible(layer, false);
            return;
        }

        sprite.LayerSetState(layer, state);
        sprite.LayerSetVisible(layer, true);
    }
}
