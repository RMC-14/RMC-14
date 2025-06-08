using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Heal;
using Content.Shared._RMC14.Xenonids.Salve;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.Heal;

public sealed class XenoSalveVisualsSystem : VisualizerSystem<XenoSalveVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, XenoSalveVisualsComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite == null || !AppearanceSystem.TryGetData(uid, XenoHealerVisuals.Gooped, out bool goop))
            return;

        if (!sprite.LayerMapTryGet(XenoHealerVisualLayers.Goop, out var layer))
            return;

        if (!goop)
        {
            sprite.LayerSetVisible(layer, false);
            return;
        }

        sprite.LayerSetVisible(layer, true);

        string layerState = "salved";

        if (AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Downed, out bool downed) && downed)
            layerState += "_downed";
        else if (AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Resting, out bool resting) && resting)
            layerState += "_rest";

        sprite.LayerSetState(layer, layerState);
    }
}
