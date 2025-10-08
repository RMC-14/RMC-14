using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hedgehog;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.Hedgehog;

public sealed class XenoShardVisualizerSystem : VisualizerSystem<XenoShardComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, XenoShardComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite == null || !AppearanceSystem.TryGetData(uid, XenoShardVisuals.Level, out XenoShardLevel level))
            return;

        // Check if entity is dead first
        if (AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Dead, out bool dead) && dead)
        {
            sprite.LayerSetState(0, "dead");
            return;
        }
        
        // Check if entity is in crit or resting state
        if (AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Downed, out bool downed) && downed)
        {
            sprite.LayerSetState(0, "crit");
            return;
        }
        
        if (AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Resting, out bool resting) && resting)
        {
            sprite.LayerSetState(0, "sleeping");
            return;
        }

        // Only use hedgehog scaling for normal alive state
        string layerState = $"hedgehog_{(int)level}";

        try
        {
            sprite.LayerSetState(0, layerState);
        }
        catch
        {
            // Fallback to alive if hedgehog sprite doesn't exist
            sprite.LayerSetState(0, "alive");
        }
    }
}