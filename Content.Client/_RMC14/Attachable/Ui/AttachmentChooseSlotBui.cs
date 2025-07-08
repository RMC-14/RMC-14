using Content.Shared._RMC14.Attachable;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Attachable.Ui;

public sealed class AttachmentChooseSlotBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private AttachableHolderChooseSlotMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<AttachableHolderChooseSlotMenu>();

        var metaQuery = EntMan.GetEntityQuery<MetaDataComponent>();
        if (metaQuery.TryGetComponent(Owner, out var metadata))
            _menu.Title = metadata.EntityName;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not AttachableHolderChooseSlotUserInterfaceState msg)
            return;

        if (_menu == null)
            return;

        _menu.UpdateMenu(msg.AttachableSlots,
            slotId =>
            {
                SendMessage(new AttachableHolderAttachToSlotMessage(slotId));
                _menu.Close();
            });
    }
}
