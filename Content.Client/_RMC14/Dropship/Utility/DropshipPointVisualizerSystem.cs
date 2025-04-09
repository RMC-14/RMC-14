using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.Utility.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Dropship.Utility;

public sealed partial class DropshipPointVisualizerSystem : VisualizerSystem<DropshipPointVisualsComponent>
{
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

        if (!spriteComp.LayerMapTryGet(DropshipPointVisualsLayers.AttachmentBase, out var attachmentBase))
            return;

        if (!spriteComp.LayerMapTryGet(DropshipPointVisualsLayers.AttachedUtility, out var attachedUtility))
        {
            spriteComp.LayerSetVisible(attachmentBase, true);
            //spriteComp.LayerSetVisible(attachedUtility, false);
            return;
        }

        if (string.IsNullOrWhiteSpace(sprite) || string.IsNullOrWhiteSpace(state))
        {
            spriteComp.LayerSetVisible(attachmentBase, true);
            spriteComp.LayerSetVisible(attachedUtility, false);
            return;
        }

        spriteComp.LayerSetSprite(attachedUtility, new SpriteSpecifier.Rsi(new ResPath(sprite), state));

        //if (Enum.TryParse<SpriteComponent.DirectionOffset>(component.DirOffset, true, out var dir))
        //spriteComp.LayerSetDirOffset(layer, dir);
        spriteComp.LayerSetVisible(attachmentBase, false);
        spriteComp.LayerSetVisible(attachedUtility, true);
    }
}
