using Content.Shared._RMC14.Overwatch;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Overwatch;

public sealed class OverwatchConsoleBui : BoundUserInterface
{
    [ViewVariables]
    private OverwatchConsoleWindow? _window;

    public OverwatchConsoleBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        if (_window != null)
            return;

        _window = new OverwatchConsoleWindow();
        TabContainer.SetTabTitle(_window.SquadMonitor, "Squad Monitor");

        Refresh();
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is OverwatchConsoleBuiState)
            Refresh();
    }

    private void Refresh()
    {
        if (_window == null || State is not OverwatchConsoleBuiState s)
            return;

        _window.Names.DisposeAllChildren();
        _window.Roles.DisposeAllChildren();

        foreach (var marine in s.Marines)
        {
            var watchButton = new Button { Text = marine.Name };
            watchButton.OnPressed += _ => SendPredictedMessage(new OverwatchConsoleWatchBuiMsg(marine.Id));
            _window.Names.AddChild(watchButton);

            _window.Roles.AddChild(new Button { Text = "Squad Leader" }); // TODO RMC14
        }
    }
}
