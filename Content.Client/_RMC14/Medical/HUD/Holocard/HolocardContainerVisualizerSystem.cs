using Content.Shared._RMC14.Medical.HUD;
using Content.Shared._RMC14.Medical.HUD.Components;
using Content.Shared.Storage;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Medical.HUD.Holocard;
public sealed partial class HolocardContainerVisualizerSystem : VisualizerSystem<HolocardContainerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, HolocardContainerComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null ||
            !AppearanceSystem.TryGetData<HolocardStatus>(uid, HolocardContainerVisuals.State, out var holocard, args.Component) ||
            !AppearanceSystem.TryGetData<bool>(uid, StorageVisuals.Open, out var open, args.Component))
            return;

        var spriteEnt = (uid, args.Sprite);

        if (!SpriteSystem.LayerMapTryGet(spriteEnt, HolocardContainerVisualLayers.Base, out var layer, false))
            return;

        if (open && component.HideOnOpen)
        {
            SpriteSystem.LayerSetVisible(spriteEnt, layer, false);
            return;
        }

        var state = component.Prefix;

        switch (holocard)
        {
            case HolocardStatus.Urgent:
                state += "_holoorange";
                break;

            case HolocardStatus.Emergency:
                state += "_holored";
                break;

            case HolocardStatus.Xeno:
                state += "_holopurple";
                break;

            case HolocardStatus.Permadead:
                state += "_holoblack";
                break;

            default:
                SpriteSystem.LayerSetVisible(spriteEnt, layer, false);
                return;
        }

        SpriteSystem.LayerSetRsiState(spriteEnt, layer, state);
        SpriteSystem.LayerSetVisible(spriteEnt, layer, true);
    }
}
