using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Inhands;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.Xenonids.Inhands;

public sealed class XenoInhandsVisualsSystem : VisualizerSystem<XenoInhandsComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    protected override void OnAppearanceChange(EntityUid uid, XenoInhandsComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite == null)
            return;

        if (!AppearanceSystem.TryGetData(uid, XenoInhandVisuals.RightHand, out string right))
            return;

        if (!AppearanceSystem.TryGetData(uid, XenoInhandVisuals.LeftHand, out string left))
            return;

        bool downed;
        bool resting;
        bool ovi;

        AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Downed, out downed);
        AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Resting, out resting);
        AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Ovipositor, out ovi);

        string name = left;
        XenoInhandVisualLayers layerDef = XenoInhandVisualLayers.Left;

        for (int i = 0; i < 2; i++)
        {
            if (i == 1)
            {
                name = right;
                layerDef = XenoInhandVisualLayers.Right;

            }

            if (!_sprite.LayerMapTryGet((uid, sprite), layerDef, out var layer, false))
                continue;

            if (name == string.Empty)
            {
                _sprite.LayerSetVisible(uid, layer, false);
            }
            else
            {
                _sprite.LayerSetVisible(uid, layer, true);

                string stateString = $"{component.Prefix}_{name}_{layerDef.ToString().ToLower()}";


                if (ovi)
                    stateString += "_" + component.Ovi;
                else if (downed)
                    stateString += "_" + component.Downed;
                else if (resting)
                    stateString += "_" + component.Resting;

                RSI? rsi = _sprite.LayerGetEffectiveRsi((uid, sprite), layer);

                if (rsi == null)
                    continue;

                rsi.TryGetState(stateString, out var state);

                if (state != null)
                {
                    _sprite.LayerSetRsiState((uid, sprite), layer, stateString);
                }
                else
                    _sprite.LayerSetVisible(uid, layer, false);
            }
        }
    }
}
