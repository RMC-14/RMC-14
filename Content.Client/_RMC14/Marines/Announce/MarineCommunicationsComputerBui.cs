using Content.Shared._RMC14.Marines.Announce;
using JetBrains.Annotations;
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
        _window.Send.OnPressed += _ => SendPredictedMessage(new MarineCommunicationsComputerMsg( Rope.Collapse(_window.Text.TextRope)));

        if (State is MarineCommunicationsComputerBuiState s)
            _window.PlanetName.Text = s.Planet;

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (_window == null)
            return;

        if (State is MarineCommunicationsComputerBuiState s)
            _window.PlanetName.Text = s.Planet;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Close();
    }
}
