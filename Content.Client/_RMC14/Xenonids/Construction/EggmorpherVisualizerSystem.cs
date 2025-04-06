using Content.Shared._RMC14.Xenonids.Construction.EggMorpher;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.Construction;

public sealed class EggmorpherVisualizerSystem : VisualizerSystem<EggMorpherComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, EggMorpherComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite == null || !AppearanceSystem.TryGetData(uid, EggmorpherOverlayVisuals.Number, out int number) ||
            !sprite.LayerMapTryGet(EggmorpherOverlayLayers.Overlay, out var layer) ||
            !sprite.LayerMapTryGet(EggmorpherOverlayLayers.Base, out var layer2))
            return;

        //Same as parasite number calc
        int level = (int)Math.Min(Math.Ceiling(((double)number / component.MaxParasites) * component.OverlayCount), component.OverlayCount);

        if (level == 0)
        {
            sprite.LayerSetVisible(layer, false);
            return;
        }

        var wasVisible = true;

        if (!sprite[layer].Visible)
        {
            sprite.LayerSetVisible(layer, true);
            wasVisible = false;
        }

        string state = component.OverlayPrefix + "_" + (level - 1);

        if (state != sprite.LayerGetState(layer) || !wasVisible)
        {
            sprite.LayerSetState(layer, state);
            var stat = sprite.LayerGetState(layer2);
            sprite.LayerSetState(layer2, state);
            sprite.LayerSetState(layer2, stat);
        }
    }
}
