using Content.Shared._RMC14.Medical.CryoCell;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Serilog;

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

        UpdateUi();
    }

    public void UpdateUi()
    {
        if (_window == null)
            return;

        if (!EntMan.TryGetComponent<CryoCellComponent>(Owner, out var cryoCell))
            return;

        try
        {
            _window.UpdateState(cryoCell);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to update Cryo Cell UI: {e}");
        }
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
