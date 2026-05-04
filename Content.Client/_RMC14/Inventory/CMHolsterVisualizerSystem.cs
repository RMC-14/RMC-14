using Content.Shared._RMC14.Inventory;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Inventory;

/// <summary>
/// Sets the gun underlay of holsters
/// </summary>
public sealed class CMHolsterVisualizerSystem : VisualizerSystem<CMHolsterComponent>
{

    protected override void OnAppearanceChange(EntityUid uid,
        CMHolsterComponent component,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite ||
            !sprite.LayerMapTryGet(CMHolsterLayers.Fill, out var layer))
            return;

        if (component.Contents.Count != 0)
        {
            // TODO: implement per-gun underlay here
            // sprite.LayerSetState(layer, $"{<gun_state_here>}");
            sprite.LayerSetVisible(layer, true);

            // TODO: account for the gunslinger belt
            return;
        }

        sprite.LayerSetVisible(layer, false);
    }
}
