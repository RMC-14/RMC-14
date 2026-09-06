using Content.Shared._RMC14.Medical.CryoCell;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Medical.CryoCell;

public sealed class CryoCellSystem : SharedCryoCellSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoCellComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<CryoCellComponent, AfterAutoHandleStateEvent>(OnComponentStateChanged);
    }

    private void OnAppearanceChange(EntityUid uid, CryoCellComponent comp, ref AppearanceChangeEvent args)
    {
        if (!_appearance.TryGetData<CryoCellVisualState>(uid, CryoCellVisuals.State, out var state))
            return;

        if (!_sprite.LayerMapTryGet((uid, args.Sprite), CryoCellVisualLayers.Base, out var baseLayer, false))
            return;

        var rsiState = state switch
        {
            CryoCellVisualState.OnEmpty => "cell-on-empty",
            CryoCellVisualState.OnOccupied => "cell-on-occupied",
            CryoCellVisualState.OffEmpty => "cell-off-empty",
            CryoCellVisualState.OffOccupied => "cell-off-occupied",
            _ => "cell-off-empty",
        };

        _sprite.LayerSetRsiState((uid, args.Sprite), baseLayer, rsiState);
        _sprite.LayerSetVisible((uid, args.Sprite), baseLayer, true);
    }

    private void OnComponentStateChanged(Entity<CryoCellComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is CryoCellBui cryoCellUi)
                    cryoCellUi.Refresh();
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error refreshing {nameof(CryoCellBui)}\n{ex}");
        }
    }
}
