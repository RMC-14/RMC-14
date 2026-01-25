using Content.Client.Message;
using Content.Shared._RMC14.Dropship;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Dropship;

[UsedImplicitly]
public sealed class DropshipHijackerBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private DropshipHijackerWindow? _window;

    protected override void Open()
    {
        base.Open();
        if (State is DropshipHijackerBuiState s)
            Set(s);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is DropshipHijackerBuiState s)
            Set(s);
    }

    private void Set(DropshipHijackerBuiState s)
    {
        if (_window == null)
        {
            _window = this.CreateWindow<DropshipHijackerWindow>();
            _window.Header.SetMarkup("[bold]Where to 'land'?[/bold]");
        }

        _window.Destinations.DisposeAllChildren();
        foreach (var (id, name) in s.Destinations)
        {
            var button = new Button
            {
                Text = name,
                StyleClasses = { "OpenBoth" }
            };

            button.OnPressed += _ =>
            {
                SendPredictedMessage(new DropshipHijackerDestinationChosenBuiMsg(id));
                Close();
            };

            _window.Destinations.AddChild(button);
        }
    }
}
