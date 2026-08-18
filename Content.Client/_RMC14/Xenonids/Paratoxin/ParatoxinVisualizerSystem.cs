using Content.Shared._RMC14.Xenonids.Paratoxin;
using Robust.Client.GameObjects;
namespace Content.Client._RMC14.Xenonids.Paratoxin;

public sealed class ParatoxinVisualizerSystem : VisualizerSystem<ParatoxinVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ParatoxinVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite == null || !AppearanceSystem.TryGetData(uid, ParatoxinVisuals.Stacks, out int stacks) ||
    !SpriteSystem.LayerMapTryGet((uid, sprite), ParatoxinVisualLayers.Base, out var layer, true))
            return;

        if (stacks <= 0)
        {
            SpriteSystem.LayerSetVisible((uid, sprite), layer, false);
            return;
        }
        else
            SpriteSystem.LayerSetVisible((uid, sprite), layer, true);

        string state = $"bub" + (stacks - 1) / 5;

        SpriteSystem.LayerSetRsiState((uid, sprite), layer, state);
    }
}
