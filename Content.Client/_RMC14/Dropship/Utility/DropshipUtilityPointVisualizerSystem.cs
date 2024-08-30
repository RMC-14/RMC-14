using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.Utility;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._RMC14.Dropship.Utility;

public sealed partial class DropshipUtilityPointVisualizerSystem : VisualizerSystem<DropshipUtilityPointComponent>
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    protected override void OnAppearanceChange(EntityUid uid, DropshipUtilityPointComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);
        if (args.Sprite is not { } spriteComp)
            return;

        if (!AppearanceSystem.TryGetData(uid, DropshipUtilityVisuals.Sprite, out string? sprite, args.Component) ||
            !AppearanceSystem.TryGetData(uid, DropshipUtilityVisuals.State, out string? state, args.Component))
        {
            return;
        }

        if (!spriteComp.LayerMapTryGet(DropshipUtilityPointLayers.Layer, out var layer))
            return;

        if (string.IsNullOrWhiteSpace(sprite) || string.IsNullOrWhiteSpace(state) || !component.WillRender)
        {
            spriteComp.LayerSetVisible(layer, false);
            return;
        }

        spriteComp.LayerSetSprite(layer, new SpriteSpecifier.Rsi(new ResPath(sprite), state));

        //if (Enum.TryParse<SpriteComponent.DirectionOffset>(component.DirOffset, true, out var dir))
            //spriteComp.LayerSetDirOffset(layer, dir);

        spriteComp.LayerSetVisible(layer, true);
    }
}
