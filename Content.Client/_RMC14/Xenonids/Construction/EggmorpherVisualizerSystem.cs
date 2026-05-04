using Content.Shared._RMC14.Xenonids.Construction.EggMorpher;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.Construction;

public sealed class EggmorpherVisualizerSystem : VisualizerSystem<EggMorpherComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    protected override void OnAppearanceChange(EntityUid uid, EggMorpherComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite == null || !AppearanceSystem.TryGetData(uid, EggmorpherOverlayVisuals.Number, out int number) ||
            !_sprite.LayerMapTryGet(uid, EggmorpherOverlayLayers.Overlay, out var layer, false) ||
            !_sprite.LayerMapTryGet(uid, EggmorpherOverlayLayers.Base, out var layer2, false))
            return;

        //Same as parasite number calc
        int level = (int)Math.Min(Math.Ceiling(((double)number / component.MaxParasites) * component.OverlayCount), component.OverlayCount);

        if (level == 0)
        {
            _sprite.LayerSetVisible(uid, layer, false);
            return;
        }

        var wasVisible = true;

        if (!_sprite.TryGetLayer(uid, layer, out var layerObj, false) || !layerObj.Visible)
        {
            _sprite.LayerSetVisible(uid, layer, true);
            wasVisible = false;
        }

        string state = component.OverlayPrefix + "_" + (level - 1);

        if (state != _sprite.LayerGetRsiState(uid, layer) || !wasVisible)
        {
            _sprite.LayerSetRsiState(uid, layer, state);
            var stat = _sprite.LayerGetRsiState(uid, layer2);
            _sprite.LayerSetRsiState(uid, layer2, state);
            _sprite.LayerSetRsiState(uid, layer2, stat);
        }
    }
}
