using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Medical.CryoCell;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Medical.CryoCell;

[UsedImplicitly]
public sealed class CryoCellEjectConfirmationBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private ConfirmationWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ConfirmationWindow>();

        _window.Setup(
            Loc.GetString("rmc-cryo-cell-eject-confirmation-title"),
            Loc.GetString("rmc-cryo-cell-eject-confirmation-text"),
            Loc.GetString("rmc-cryo-cell-eject-confirmation-confirm"),
            Loc.GetString("rmc-cryo-cell-eject-confirmation-cancel")
        );

        _window.AcceptButton.OnPressed += _ =>
        {
            SendPredictedMessage(new CryoCellEjectConfirmationBuiMsg(true));
            _window?.Close();
        };

        _window.DenyButton.OnPressed += _ =>
        {
            SendPredictedMessage(new CryoCellEjectConfirmationBuiMsg(false));
            _window?.Close();
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _window?.Close();
            _window = null;
        }

        base.Dispose(disposing);
    }
}
