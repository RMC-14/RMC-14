using Content.Shared._RMC14.Chemistry.ChemMaster;
using Content.Shared._RMC14.Chemistry.PillBottle;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Chemistry.PillBottle;

public sealed class RMCBreakablePillBottleVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCBreakablePillBottleComponent, AppearanceChangeEvent>(
            OnAppearanceChange,
            after: [typeof(GenericVisualizerSystem)]);
    }

    private void OnAppearanceChange(Entity<RMCBreakablePillBottleComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var sprite = (ent.Owner, args.Sprite);
        _appearance.TryGetData(ent, RMCPillBottleVisuals.Broken, out bool broken, args.Component);

        if (!broken && !_sprite.LayerMapTryGet(sprite, RMCPillBottleLayers.Broken, out _, false))
            return;

        var layer = _sprite.LayerMapReserve(sprite, RMCPillBottleLayers.Broken);
        _sprite.LayerSetRsiState(sprite, layer, ent.Comp.BrokenState);
        _sprite.LayerSetVisible(sprite, layer, broken);

        if (!broken)
            return;

        foreach (var lid in ent.Comp.HiddenLayersWhenBroken)
        {
            if (_sprite.LayerMapTryGet(sprite, lid, out var lidLayer, false))
                _sprite.LayerSetVisible(sprite, lidLayer, false);
        }
    }
}
