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

        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out CryoCellComponent? cryoCell))
            return;

        _window.UpdateFromComponent(cryoCell);
    }

    public void TogglePower()
    {
        SendPredictedMessage(new CryoCellTogglePowerBuiMsg());
    }

    public void ToggleAutoEject()
    {
        SendPredictedMessage(new CryoCellToggleAutoEjectBuiMsg());
    }

    public void Eject()
    {
        SendPredictedMessage(new CryoCellEjectBuiMsg());
    }

    public void EjectBeaker()
    {
        SendPredictedMessage(new CryoCellEjectBeakerBuiMsg());
    }

    public void ToggleNotify()
    {
        SendPredictedMessage(new CryoCellToggleNotifyBuiMsg());
    }
}
