using Content.Client._RMC14.UserInterface;
using Content.Client.Eui;
using Content.Shared._RMC14.Medical.CryoCell;
using JetBrains.Annotations;

namespace Content.Client._RMC14.Medical.CryoCell;

[UsedImplicitly]
public sealed class CryoCellEjectEui : BaseEui
{
    private readonly ConfirmationWindow _window;

    public CryoCellEjectEui()
    {
        _window = new ConfirmationWindow();

        _window.Setup(
            Loc.GetString("rmc-cryo-cell-eject-confirmation-title"),
            Loc.GetString("rmc-cryo-cell-eject-confirmation-text"),
            Loc.GetString("rmc-cryo-cell-eject-confirmation-confirm"),
            Loc.GetString("rmc-cryo-cell-eject-confirmation-cancel")
        );

        _window.AcceptButton.OnPressed += _ =>
        {
            SendMessage(new CryoCellEjectConfirmationMessage(true));
            _window.Close();
        };

        _window.DenyButton.OnPressed += _ =>
        {
            SendMessage(new CryoCellEjectConfirmationMessage(false));
            _window.Close();
        };
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }
}
