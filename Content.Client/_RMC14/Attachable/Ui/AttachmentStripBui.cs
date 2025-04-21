using Content.Shared._RMC14.Attachable;

namespace Content.Client._RMC14.Attachable.Ui;

public sealed class AttachmentStripBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private AttachableHolderStripMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = new AttachableHolderStripMenu();

        var metaQuery = EntMan.GetEntityQuery<MetaDataComponent>();
        if (metaQuery.TryGetComponent(Owner, out var metadata))
            _menu.Title = metadata.EntityName;

        _menu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not AttachableHolderStripUserInterfaceState msg)
            return;

        if (_menu == null)
            return;

        _menu.UpdateMenu(msg.AttachableSlots, slotId => SendMessage(new AttachableHolderDetachMessage(slotId)));
    }
}
