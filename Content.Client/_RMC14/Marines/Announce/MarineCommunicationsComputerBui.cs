using Content.Shared._RMC14.Marines.Announce;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Marines.Announce;

[UsedImplicitly]
public sealed class MarineCommunicationsComputerBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private MarineCommunicationsComputerWindow? _window;

    protected override void Open()
    {
        if (_window != null)
            return;

        _window = new MarineCommunicationsComputerWindow();
        _window.TacticalMapButton.OnPressed += _ => SendPredictedMessage(new MarineCommunicationsOpenMapMsg());
        _window.OverwatchButton.OnPressed += _ => SendPredictedMessage(new MarineCommunicationsOverwatchMsg());
        _window.Send.OnPressed += _ => SendPredictedMessage(new MarineCommunicationsComputerMsg( Rope.Collapse(_window.Text.TextRope)));
        OnStateUpdate();

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        OnStateUpdate();
    }

    private void OnStateUpdate()
    {
        if (_window == null)
            return;

        if (State is MarineCommunicationsComputerBuiState s)
        {
            _window.LandingZonesContainer.DisposeAllChildren();
            _window.PlanetName.Text = s.Planet;
            _window.OperationName.Text = s.Operation;

            foreach (var zone in s.LandingZones)
            {
                var button = new Button { Text = zone.Name };
                button.OnPressed += _ => SendPredictedMessage(new MarineCommunicationsDesignatePrimaryLZMsg(zone.Id));
                _window.LandingZonesContainer.AddChild(button);
            }

            _window.LandingZonesSection.Visible = s.LandingZones.Count > 0;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Close();
    }
}
