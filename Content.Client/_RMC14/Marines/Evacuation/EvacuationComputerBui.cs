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
                _window.StatusLabel.Text = "Escape Pod Status: DELAYED";
                _window.HatchLabel.Text = "Docking Hatch: UNSECURED";
                break;
            case EvacuationComputerMode.Ready:
                _window.StatusLabel.Text = "Escape Pod Status: STANDING BY";
                _window.HatchLabel.Text = "Docking Hatch: SECURED";
                break;
            case EvacuationComputerMode.Travelling:
                // TODO RMC14 launching
                _window.StatusLabel.Text = "Escape Pod Status: TRAVELLING";
                _window.HatchLabel.Text = "Docking Hatch: SECURED";
                break;
            case EvacuationComputerMode.Crashed:
                _window.StatusLabel.Text = "Escape Pod Status: CRASHED";
                _window.HatchLabel.Text = "Docking Hatch: UNSECURED";
                break;
        }
    }
}
