using Content.Shared._RMC14.Medical.CryoCell;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Medical.CryoCell;

[UsedImplicitly]
public sealed class CryoCellBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private CryoCellWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CryoCellWindow>();
        _window.Title = Loc.GetString("rmc-cryo-cell-window-title");
        _window.SetBui(this);

        if (EntMan.TryGetComponent(Owner, out CryoCellComponent? cryoCell))
            _window.UpdateFromComponent(cryoCell);

        EntMan.System<CryoCellSystem>().SetWindow(_window, Owner);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            EntMan.System<CryoCellSystem>().SetWindow(null, EntityUid.Invalid);

        base.Dispose(disposing);
    }

    public void TogglePower()
    {
        SendMessage(new CryoCellTogglePowerBuiMsg());
    }

    public void ToggleAutoEject()
    {
        SendMessage(new CryoCellToggleAutoEjectBuiMsg());
    }

    public void Eject()
    {
        SendMessage(new CryoCellEjectBuiMsg());
    }

    public void EjectBeaker()
    {
        SendMessage(new CryoCellEjectBeakerBuiMsg());
    }

    public void ToggleNotify()
    {
        SendMessage(new CryoCellToggleNotifyBuiMsg());
    }
}
