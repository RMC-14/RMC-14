using Content.Shared._RMC14.Xenonids.Fruit.Components;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.Fruit;

// System for updating the sprite of a fruit planter based on their selected fruit
public sealed class XenoFruitPlanterVisualsSystem : VisualizerSystem<XenoFruitPlanterVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, XenoFruitPlanterVisualsComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite == null ||
            !AppearanceSystem.TryGetData(uid, XenoFruitPlanterVisuals.Color, out Color color) ||
            !sprite.LayerMapTryGet(XenoFruitVisualLayers.Base, out var layer))
        {
            return;
        }

        if (color == null)
        {
            sprite.LayerSetVisible(layer, false);
            return;
        }

        sprite.LayerSetVisible(layer, true);
        sprite.LayerSetColor(layer, color);

        if (AppearanceSystem.TryGetData(uid, XenoFruitPlanterVisuals.Downed, out bool downed) && downed)
        {
            sprite.LayerSetState(layer, $"{component.Prefix}_downed");
            return;
        }

        if (AppearanceSystem.TryGetData(uid, XenoFruitPlanterVisuals.Resting, out bool resting) && resting)
        {
            sprite.LayerSetState(layer, $"{component.Prefix}_rest");
            return;
        }

        sprite.LayerSetState(layer, $"{component.Prefix}_walk");
    }
}
