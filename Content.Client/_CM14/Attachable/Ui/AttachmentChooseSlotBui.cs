using Content.Shared._CM14.Attachable;

namespace Content.Client._CM14.Attachable.Ui;

public sealed class AttachmentChooseSlotBui : BoundUserInterface
{
    [ViewVariables]
    private AttachableHolderChooseSlotMenu? _menu;

    public AttachmentChooseSlotBui(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _menu = new AttachableHolderChooseSlotMenu(this);

        var metaQuery = EntMan.GetEntityQuery<MetaDataComponent>();
        if (metaQuery.TryGetComponent(Owner, out var metadata))
            _menu.Title = metadata.EntityName;

        _menu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not AttachableHolderChooseSlotUserInterfaceState msg)
            return;

        if (_menu == null)
            return;

        _menu.UpdateMenu(msg.AttachableSlots);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _menu?.Dispose();
    }
}
