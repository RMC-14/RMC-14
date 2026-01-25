using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Inhands;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.Xenonids.Inhands;

public sealed class XenoInhandsVisualsSystem : VisualizerSystem<XenoInhandsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, XenoInhandsComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite == null)
            return;

        if (!AppearanceSystem.TryGetData(uid, XenoInhandVisuals.RightHand, out string right))
            return;

        if (!AppearanceSystem.TryGetData(uid, XenoInhandVisuals.LeftHand, out string left))
            return;

        bool downed = false;
        bool resting = false;
        bool ovi = false;

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

            if (!sprite.LayerMapTryGet(layerDef, out var layer))
                continue;

            if (name == string.Empty)
            {
                sprite.LayerSetVisible(layer, false);
            }
            else
            {
                sprite.LayerSetVisible(layer, true);

                string stateString = $"{component.Prefix}_{name}_{layerDef.ToString().ToLower()}";


                if (ovi)
                    stateString += "_" + component.Ovi;
                else if (downed)
                    stateString += "_" + component.Downed;
                else if (resting)
                    stateString += "_" + component.Resting;

                RSI? rsi = sprite.LayerGetActualRSI(layerDef);

                if (rsi == null)
                    continue;

                rsi.TryGetState(stateString, out var state);

                if (state != null)
                {
                    sprite.LayerSetState(layer, stateString);
                }
                else
                    sprite.LayerSetVisible(layer, false);
            }
        }
    }
}
