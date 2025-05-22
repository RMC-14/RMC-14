using Content.Client.UserInterface.Controls;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.ControlComputer;
using Content.Shared._RMC14.Overwatch;
using Content.Shared._RMC14.TacticalMap;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Marines.Announce;

[UsedImplicitly]
public sealed class MarineCommunicationsComputerBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private MarineCommunicationsComputerWindow? _window;

    protected override void Open()
    {
        base.Open();
        if (_window != null)
            return;

        _window = this.CreateWindow<MarineCommunicationsComputerWindow>();

        if (EntMan.TryGetComponent(Owner, out MarineCommunicationsComputerComponent? communications) &&
            communications.CanGiveMedals)
        {
            _window.MedalButton.OnPressed += _ => SendPredictedMessage(new MarineControlComputerMedalMsg());
            _window.MedalButton.Visible = true;
        }
        else
        {
            _window.MedalButton.Visible = false;
        }

        if (EntMan.HasComponent<TacticalMapComputerComponent>(Owner))
            _window.TacticalMapButton.OnPressed += _ => SendPredictedMessage(new MarineCommunicationsOpenMapMsg());
        else
            _window.TacticalMapButton.Visible = false;

        if (EntMan.TryGetComponent<MarineCommunicationsComputerComponent>(Owner, out var computer) &&
            computer.CanCreateEcho)
        {
            _window.EchoButton.OnPressed += _ => SendPredictedMessage(new MarineCommunicationsEchoSquadMsg());
        }

        if (EntMan.HasComponent<OverwatchConsoleComponent>(Owner))
            _window.OverwatchButton.OnPressed += _ => SendPredictedMessage(new MarineCommunicationsOverwatchMsg());
        else
            _window.OverwatchButton.Visible = false;

        _window.Send.OnPressed += _ => SendPredictedMessage(new MarineCommunicationsComputerMsg( Rope.Collapse(_window.Text.TextRope)));
        OnStateUpdate();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        OnStateUpdate();
    }

    public void OnStateUpdate()
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
                var button = new ConfirmButton
                {
                    Text = zone.Name,
                    StyleClasses = { "OpenBoth" },
                };
                button.OnPressed += _ => SendPredictedMessage(new MarineCommunicationsDesignatePrimaryLZMsg(zone.Id));
                _window.LandingZonesContainer.AddChild(button);
            }

            _window.LandingZonesSection.Visible = s.LandingZones.Count > 0;
        }

        _window.EchoButton.Visible =
            EntMan.TryGetComponent<MarineCommunicationsComputerComponent>(Owner, out var computer) &&
            computer.CanCreateEcho;
        _window.EchoSeparator.Visible = _window.EchoButton.Visible;
    }
}
