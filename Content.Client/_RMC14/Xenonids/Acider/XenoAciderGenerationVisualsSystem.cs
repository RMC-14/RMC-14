using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.AciderGeneration;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.Acider;

public sealed class XenoAciderGenerationVisualsSystem : VisualizerSystem<XenoAciderGenerationComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    protected override void OnAppearanceChange(EntityUid uid, XenoAciderGenerationComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite == null || !AppearanceSystem.TryGetData(uid, XenoAcidGeneratingVisuals.Generating, out bool gening))
            return;

        if (!_sprite.LayerMapTryGet(uid, XenoAcidGeneratingVisualLayers.Base, out var layer, false))
            return;

        if (!gening)
        {
            _sprite.LayerSetVisible(uid, layer, false);
            return;
        }

        _sprite.LayerSetVisible(uid, layer, true);

        string layerState = "acid";

        if (AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Downed, out bool downed) && downed)
            layerState += "_downed";
        else if (AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Resting, out bool resting) && resting)
            layerState += "_rest";

        _sprite.LayerSetRsiState(uid, layer, layerState);
    }
}
