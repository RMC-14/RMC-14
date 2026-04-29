using Content.Shared._RMC14.Ghost;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Damage;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Ghost;

public sealed class GhostXenoAppearanceVisualizerSystem : VisualizerSystem<GhostXenoAppearanceComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, GhostXenoAppearanceComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;
        if (sprite == null)
            return;

        SpriteSystem.LayerSetRsi((uid, sprite), XenoVisualLayers.Base, component.Sprite);
        var rsi = SpriteSystem.LayerGetEffectiveRsi((uid, sprite), XenoVisualLayers.Base, "alive");
        if (rsi != null && rsi.TryGetState("alive", out _))
            sprite.LayerSetState(XenoVisualLayers.Base, "alive");

        if (sprite.LayerMapTryGet(XenoVisualLayers.Ovipositor, out var oviLayer))
            sprite.LayerSetVisible(oviLayer, false);

        if (sprite.LayerMapTryGet(RMCDamageVisualLayers.Base, out var damageLayer))
            sprite.LayerSetVisible(damageLayer, false);

        sprite.LayerSetVisible(XenoVisualLayers.Base, true);
    }
}
