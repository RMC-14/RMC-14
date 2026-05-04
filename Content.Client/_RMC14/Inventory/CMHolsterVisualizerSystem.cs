using Content.Shared._RMC14.Inventory;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Inventory;

/// <summary>
/// Sets the gun underlay of holsters
/// </summary>
public sealed class CMHolsterVisualizerSystem : VisualizerSystem<CMHolsterComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    protected override void OnAppearanceChange(EntityUid uid,
        CMHolsterComponent component,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite ||
            !_sprite.LayerMapTryGet((uid, sprite), CMHolsterLayers.Fill, out var layer, false))
            return;

        if (component.Contents.Count != 0)
        {
            // TODO: implement per-gun underlay here
            // sprite.LayerSetState(layer, $"{<gun_state_here>}");
            _sprite.LayerSetVisible((uid, sprite), layer, true);

            // TODO: account for the gunslinger belt
            return;
        }

        _sprite.LayerSetVisible((uid, sprite), layer, false);
    }
}
