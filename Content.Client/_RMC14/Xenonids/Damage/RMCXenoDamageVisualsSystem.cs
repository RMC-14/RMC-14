using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Damage;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.Damage;

public sealed class RMCXenoDamageVisualsSystem : VisualizerSystem<RMCXenoDamageVisualsComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    protected override void OnAppearanceChange(EntityUid uid, RMCXenoDamageVisualsComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;
        if (sprite == null ||
            !AppearanceSystem.TryGetData(uid, RMCDamageVisuals.State, out int level) ||
            !_sprite.LayerMapTryGet(uid, RMCDamageVisualLayers.Base, out var layer, false))
        {
            return;
        }

        if (level == 0)
        {
            _sprite.LayerSetVisible(uid, layer, false);
            return;
        }

        _sprite.LayerSetVisible(uid, layer, true);

        var state = component.States - level + 1;
        if (AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Downed, out bool downed) && downed)
        {
            _sprite.LayerSetRsiState(uid, layer, $"{component.Prefix}_downed_{state}");
            return;
        }

        if (AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Fortified, out bool fortified) && fortified)
        {
            _sprite.LayerSetRsiState(uid, layer, $"{component.Prefix}_fortify_{state}");
            return;
        }

        if (AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Resting, out bool resting) && resting)
        {
            _sprite.LayerSetRsiState(uid, layer, $"{component.Prefix}_rest_{state}");
            return;
        }

        _sprite.LayerSetRsiState(uid, layer, $"{component.Prefix}_walk_{state}");
    }
}
