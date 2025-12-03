using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hedgehog;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.Hedgehog;

public sealed class XenoShardVisualizerSystem : VisualizerSystem<XenoShardComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, XenoShardComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite == null || !AppearanceSystem.TryGetData(uid, XenoShardVisuals.Level, out XenoShardLevel level) ||
            !SpriteSystem.LayerMapTryGet((uid, sprite), XenoShardVisualLayers.Base, out var layer, true))
            return;

        string layerState = $"hedgehog_{(int)level}";

        // Check if entity is dead first
        if (AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Dead, out bool dead) && dead)
        {
            return;
        }
        else if (AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Downed, out bool downed) && downed)
        {
            layerState += "_crit";
        }
        else if (AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Resting, out bool resting) && resting)
        {
            layerState += "_resting";
        }

        SpriteSystem.LayerSetRsiState((uid, sprite), layer, layerState);
    }
}
