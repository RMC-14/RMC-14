using Content.Shared._RMC14.Medical.CryoCell;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Medical.CryoCell;

public sealed class CryoCellSystem : SharedCryoCellSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoCellComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<CryoCellComponent, AfterAutoHandleStateEvent>(OnCryoCellAfterState);

        SubscribeLocalEvent<InsideCryoCellComponent, MoveInputEvent>(OnInsideCryoCellMoveInput);
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

    private void OnCryoCellAfterState(Entity<CryoCellComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateCryoCellUI(ent);
    }

    private void UpdateCryoCellUI(Entity<CryoCellComponent> ent)
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

    protected override void RequestEjectConfirmation(EntityUid cell)
    {
        RaiseNetworkEvent(new CryoCellEjectConfirmationRequestEvent
        {
            CryoCell = GetNetEntity(cell),
        });
    }

    private void OnInsideCryoCellMoveInput(Entity<InsideCryoCellComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Chamber is not { } cellId)
            return;

        if (_mobState.IsIncapacitated(ent))
            return;

        RaiseNetworkEvent(new CryoCellEjectConfirmationRequestEvent
        {
            CryoCell = GetNetEntity(cellId),
        });
    }
}
