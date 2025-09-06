using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Damage;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.Damage;

public sealed class RMCXenoDamageVisualsSystem : VisualizerSystem<RMCXenoDamageVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, RMCXenoDamageVisualsComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;
        if (sprite == null ||
            !AppearanceSystem.TryGetData(uid, RMCDamageVisuals.State, out int level) ||
            !sprite.LayerMapTryGet(RMCDamageVisualLayers.Base, out var layer))
        {
            return;
        }

        if (level == 0)
        {
            sprite.LayerSetVisible(layer, false);
            return;
        }

        sprite.LayerSetVisible(layer, true);

        var state = component.States - level + 1;
        if (AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Downed, out bool downed) && downed)
        {
            sprite.LayerSetState(layer, $"{component.Prefix}_downed_{state}");
            return;
        }

        if (AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Fortified, out bool fortified) && fortified)
        {
            sprite.LayerSetState(layer, $"{component.Prefix}_fortify_{state}");
            return;
        }

        if (AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Resting, out bool resting) && resting)
        {
            sprite.LayerSetState(layer, $"{component.Prefix}_rest_{state}");
            return;
        }

        sprite.LayerSetState(layer, $"{component.Prefix}_walk_{state}");
    }
}
