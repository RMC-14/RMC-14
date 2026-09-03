using Robust.Client.GameObjects;
using Content.Shared._RMC14.Medical.CryoCell;

namespace Content.Client._RMC14.Medical.CryoCell;

public sealed class CryoCellSystem : SharedCryoCellSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private ISawmill _sawmill = default!;
    private CryoCellWindow? _window;
    private EntityUid _windowOwner;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("cryo_cell");

        SubscribeLocalEvent<CryoCellComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CryoCellComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<CryoCellComponent, AfterAutoHandleStateEvent>(OnComponentStateChanged);
    }

    private void OnInit(EntityUid uid, CryoCellComponent comp, ComponentInit args)
    {
        UpdateCryoCellAppearance(uid, comp);
    }

    private void OnAppearanceChange(EntityUid uid, CryoCellComponent comp, ref AppearanceChangeEvent args)
    {
        UpdateCryoCellAppearance(uid, comp, args.Sprite);
    }

    private void OnComponentStateChanged(Entity<CryoCellComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (_window != null &&
                ent.Owner == _windowOwner)
            {
                _window.UpdateFromComponent(ent.Comp);
            }
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Error updating Cryo Cell UI on state change: {ex}");
        }
    }

    public void SetWindow(CryoCellWindow? window, EntityUid owner)
    {
        _window = window;
        _windowOwner = owner;
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
