using Content.Shared._RMC14.Weapons.Ranged;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Weapons.Ranged;

public sealed class BulletholeVisualizerSystem : VisualizerSystem<BulletholeComponent>
{
    private const string BulletholeRsiPath = "/Textures/_RMC14/Effects/bulletholes.rsi";

    protected override void OnAppearanceChange(EntityUid uid, BulletholeComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        if (!AppearanceSystem.TryGetData<string>(uid, BulletholeVisuals.State, out var state, args.Component))
            return;

        if (!sprite.LayerMapTryGet(BulletholeVisualsLayers.Bullethole, out var layer))
            layer = sprite.LayerMapReserveBlank(BulletholeVisualsLayers.Bullethole);

        var valid = !string.IsNullOrWhiteSpace(state);

        args.Sprite.LayerSetVisible(BulletholeVisualsLayers.Bullethole, valid);

        if (valid)
        {
            args.Sprite.LayerSetRSI(BulletholeVisualsLayers.Bullethole, BulletholeRsiPath);
            args.Sprite.LayerSetState(BulletholeVisualsLayers.Bullethole, state);
        }
    }
}

