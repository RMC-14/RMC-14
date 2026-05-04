using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.Utility.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Dropship.Utility;

public sealed partial class DropshipPointVisualizerSystem : VisualizerSystem<DropshipPointVisualsComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    protected override void OnAppearanceChange(EntityUid uid, DropshipPointVisualsComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);
        if (args.Sprite is not { } spriteComp)
            return;

        if (!AppearanceSystem.TryGetData(uid, DropshipUtilityVisuals.Sprite, out string? sprite, args.Component) ||
            !AppearanceSystem.TryGetData(uid, DropshipUtilityVisuals.State, out string? state, args.Component))
        {
            return;
        }

        if (!_sprite.LayerMapTryGet(new Entity<SpriteComponent?>(uid, spriteComp), DropshipPointVisualsLayers.AttachmentBase, out var attachmentBase, false))
            return;

        if (!_sprite.LayerMapTryGet(new Entity<SpriteComponent?>(uid, spriteComp), DropshipPointVisualsLayers.AttachedUtility, out var attachedUtility, false))
        {
            _sprite.LayerSetVisible(new Entity<SpriteComponent?>(uid, spriteComp), attachmentBase, true);
            //spriteComp.LayerSetVisible(attachedUtility, false);
            return;
        }

        if (string.IsNullOrWhiteSpace(sprite) || string.IsNullOrWhiteSpace(state))
        {
            _sprite.LayerSetVisible(new Entity<SpriteComponent?>(uid, spriteComp), attachmentBase, true);
            _sprite.LayerSetVisible(new Entity<SpriteComponent?>(uid, spriteComp), attachedUtility, false);
            return;
        }

        _sprite.LayerSetSprite(new Entity<SpriteComponent?>(uid, spriteComp), attachedUtility, new SpriteSpecifier.Rsi(new ResPath(sprite), state));

        //if (Enum.TryParse<SpriteComponent.DirectionOffset>(component.DirOffset, true, out var dir))
        //spriteComp.LayerSetDirOffset(layer, dir);
        _sprite.LayerSetVisible(new Entity<SpriteComponent?>(uid, spriteComp), attachmentBase, false);
        _sprite.LayerSetVisible(new Entity<SpriteComponent?>(uid, spriteComp), attachedUtility, true);
    }
}
