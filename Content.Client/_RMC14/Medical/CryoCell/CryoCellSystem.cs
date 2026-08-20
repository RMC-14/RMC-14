using Content.Shared._RMC14.Medical.CryoCell;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;

namespace Content.Client._RMC14.Medical.CryoCell;

public sealed class CryoCellUISystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoCellComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<CryoCellComponent, EntInsertedIntoContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<CryoCellComponent, EntRemovedFromContainerMessage>(OnContainerChanged);
    }

    private void OnAfterAutoHandleState(Entity<CryoCellComponent> cryoCell, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(cryoCell);
    }

    private void OnContainerChanged(Entity<CryoCellComponent> cryoCell, ref EntInsertedIntoContainerMessage args)
    {
        UpdateUi(cryoCell);
    }

    private void OnContainerChanged(Entity<CryoCellComponent> cryoCell, ref EntRemovedFromContainerMessage args)
    {
        UpdateUi(cryoCell);
    }

    private void UpdateUi(Entity<CryoCellComponent> cryoCell)
    {
        if (!_ui.TryGetOpenUi(cryoCell.Owner, CryoCellUIKey.Key, out var bui))
        {
            return;
        }

        if (bui is not CryoCellBui cryoCellBui)
            return;

        try
        {
            cryoCellBui.UpdateUi();
        }
        catch (Exception e)
        {
            Log.Error($"Failed to update Cryo Cell UI: {e}");
        }
    }
}
