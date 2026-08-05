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

        if (State is CryoCellBuiState state)
            _window.UpdateState(state);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is CryoCellBuiState cryoState)
            _window?.UpdateState(cryoState);
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
