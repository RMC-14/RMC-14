using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hedgehog;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.Hedgehog;

public sealed class XenoShardVisualizerSystem : VisualizerSystem<XenoShardComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, XenoShardComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite == null || !AppearanceSystem.TryGetData(uid, XenoShardVisuals.Level, out int level))
            return;

        string layerState = $"hedgehog_{level}";

        if (AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Downed, out bool downed) && downed)
            layerState += "_crit";
        else if (AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Resting, out bool resting) && resting)
            layerState += "_resting";

        sprite.LayerSetState(0, layerState);
    }
}