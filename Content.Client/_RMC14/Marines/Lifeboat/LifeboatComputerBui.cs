using Content.Shared._RMC14.Evacuation;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Marines.Lifeboat;

[UsedImplicitly]
public sealed class LifeboatComputerBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private LifeboatComputerWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<LifeboatComputerWindow>();
        _window.EmergencyLaunchButton.OnPressed += _ => SendPredictedMessage(new LifeboatComputerLaunchBuiMsg());
        _window.NoButton.OnPressed += _ => _window.Close();
        _window.YesButton.OnPressed += _ => SendPredictedMessage(new LifeboatComputerLaunchBuiMsg());
    }
}
