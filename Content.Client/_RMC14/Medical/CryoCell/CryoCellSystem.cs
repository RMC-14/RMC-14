using Robust.Client.GameObjects;
using Content.Shared._RMC14.Medical.CryoCell;

namespace Content.Client._RMC14.Medical.CryoCell;

public sealed class CryoCellSystem : SharedCryoCellSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoCellComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CryoCellComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnInit(EntityUid uid, CryoCellComponent comp, ComponentInit args)
    {
        UpdateCryoCellAppearance(uid, comp);
    }

    private void OnAppearanceChange(EntityUid uid, CryoCellComponent comp, ref AppearanceChangeEvent args)
    {
        UpdateCryoCellAppearance(uid, comp, args.Sprite);
    }

    private void UpdateCryoCellAppearance(EntityUid uid, CryoCellComponent comp, SpriteComponent? sprite = null)
    {
        if (sprite == null && !TryComp(uid, out sprite))
            return;

        if (!_sprite.LayerMapTryGet((uid, sprite), CryoCellVisualLayers.Base, out var baseLayer, false))
            return;

        var rsiState = comp.Occupant != null
            ? (comp.IsPoweredOn ? "cell-on-occupied" : "cell-off-occupied")
            : (comp.IsPoweredOn ? "cell-on-empty" : "cell-off-empty");

        _sprite.LayerSetRsiState((uid, sprite), baseLayer, rsiState);
        _sprite.LayerSetVisible((uid, sprite), baseLayer, true);
    }
}
