using Content.Shared._RMC14.Vehicle;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Vehicle;

public sealed class RMCVehicleHardpointVisualizerSystem : VisualizerSystem<RMCVehicleHardpointVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, RMCVehicleHardpointVisualsComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;
        if (sprite == null)
            return;

        UpdateLayer(uid, sprite, RMCVehicleHardpointLayers.Primary, RMCVehicleHardpointVisuals.PrimaryState);
        UpdateLayer(uid, sprite, RMCVehicleHardpointLayers.Secondary, RMCVehicleHardpointVisuals.SecondaryState);
        UpdateLayer(uid, sprite, RMCVehicleHardpointLayers.Support, RMCVehicleHardpointVisuals.SupportState);
    }

    private void UpdateLayer(EntityUid uid, SpriteComponent sprite, string layerMap, RMCVehicleHardpointVisuals key)
    {
        if (!sprite.LayerMapTryGet(layerMap, out var layer))
            return;

        if (!AppearanceSystem.TryGetData(uid, key, out string? state) || string.IsNullOrWhiteSpace(state))
        {
            sprite.LayerSetVisible(layer, false);
            return;
        }

        sprite.LayerSetState(layer, state);
        sprite.LayerSetVisible(layer, true);
    }
}
