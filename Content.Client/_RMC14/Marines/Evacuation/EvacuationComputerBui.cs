using Content.Shared._RMC14.Evacuation;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Marines.Evacuation;

[UsedImplicitly]
public sealed class EvacuationComputerBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private EvacuationComputerWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<EvacuationComputerWindow>();

        _window.DoorButton.Visible = false; // TODO RMC14
        _window.EjectButton.OnPressed += _ => SendPredictedMessage(new EvacuationComputerLaunchBuiMsg());
        _window.DelayButton.Visible = false; // TODO RMC14

        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out EvacuationComputerComponent? computer))
            return;

        switch (computer.Mode)
        {
            case EvacuationComputerMode.Disabled:
                _window.StatusLabel.Text = Loc.GetString("rmc-evacuation-computer-status-delayed");
                _window.HatchLabel.Text = Loc.GetString("rmc-evacuation-computer-hatch-unsecured");
                break;
            case EvacuationComputerMode.Ready:
                _window.StatusLabel.Text = Loc.GetString("rmc-evacuation-computer-status-standing-by");
                _window.HatchLabel.Text = Loc.GetString("rmc-evacuation-computer-hatch-secured");
                break;
            case EvacuationComputerMode.Travelling:
                // TODO RMC14 launching
                _window.StatusLabel.Text = Loc.GetString("rmc-evacuation-computer-status-travelling");
                _window.HatchLabel.Text = Loc.GetString("rmc-evacuation-computer-hatch-secured");
                break;
            case EvacuationComputerMode.Crashed:
                _window.StatusLabel.Text = Loc.GetString("rmc-evacuation-computer-status-crashed");
                _window.HatchLabel.Text = Loc.GetString("rmc-evacuation-computer-hatch-unsecured");
                break;
        }
    }
}
