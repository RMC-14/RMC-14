using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.Weapon;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Dropship.Weapon;

public sealed class DropshipWeaponPointVisualizerSystem : VisualizerSystem<DropshipWeaponPointComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

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

        if (!_sprite.LayerMapTryGet(new Entity<SpriteComponent?>(uid, spriteComp), DropshipWeaponPointLayers.Layer, out var layer, false))
            return;

        if (string.IsNullOrWhiteSpace(sprite) || string.IsNullOrWhiteSpace(state))
        {
            _sprite.LayerSetVisible(new Entity<SpriteComponent?>(uid, spriteComp), layer, false);
            return;
        }

        _sprite.LayerSetSprite(new Entity<SpriteComponent?>(uid, spriteComp), layer, new SpriteSpecifier.Rsi(new ResPath(sprite), state));

        if (Enum.TryParse<SpriteComponent.DirectionOffset>(component.DirOffset, true, out var dir))
            _sprite.LayerSetDirOffset(new Entity<SpriteComponent?>(uid, spriteComp), layer, dir);

        _sprite.LayerSetVisible(new Entity<SpriteComponent?>(uid, spriteComp), layer, true);
    }
}
