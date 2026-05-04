using Content.Shared._RMC14.Weapons.Ranged;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Weapons.Ranged;

public sealed class BulletholeVisualizerSystem : VisualizerSystem<BulletholeComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string BulletholeRsiPath = "/Textures/_RMC14/Effects/bulletholes.rsi";

    protected override void OnAppearanceChange(EntityUid uid, BulletholeComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        if (!AppearanceSystem.TryGetData<string>(uid, BulletholeVisuals.State, out var state, args.Component))
            return;

        if (!_sprite.LayerMapTryGet((uid, sprite), BulletholeVisualsLayers.Bullethole, out _, false))
            _sprite.LayerMapReserve((uid, sprite), BulletholeVisualsLayers.Bullethole);

        var valid = !string.IsNullOrWhiteSpace(state);

        _sprite.LayerSetVisible((uid, sprite), BulletholeVisualsLayers.Bullethole, valid);

        if (valid)
        {
            _sprite.LayerSetRsi((uid, sprite), BulletholeVisualsLayers.Bullethole, new ResPath(BulletholeRsiPath));
            _sprite.LayerSetRsiState((uid, sprite), BulletholeVisualsLayers.Bullethole, state);
        }
    }
}

