using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Dropship.Weapon;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Dropship.Weapon;

public sealed class DropshipWeaponVisualizerSystem : VisualizerSystem<DropshipWeaponPointComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, DropshipWeaponPointComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);
        if (args.Sprite is not { } spriteComp)
            return;

        if (!AppearanceSystem.TryGetData(uid, DropshipWeaponVisuals.Sprite, out string? sprite, args.Component) ||
            !AppearanceSystem.TryGetData(uid, DropshipWeaponVisuals.State, out string? state, args.Component))
        {
            return;
        }

        if (!spriteComp.LayerMapTryGet(DropshipWeaponPointLayers.Layer, out var layer))
            return;

        spriteComp.LayerSetSprite(layer, new SpriteSpecifier.Rsi(new ResPath(sprite), state));
    }
}
