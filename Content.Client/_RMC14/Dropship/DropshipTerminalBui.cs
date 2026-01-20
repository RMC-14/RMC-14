using Content.Client.Message;
using Content.Shared._RMC14.Dropship;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Dropship;

[UsedImplicitly]
public sealed class DropshipTerminalBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private DropshipTerminalWindow? _window;

    private readonly Dictionary<DropshipButton, string> _dropships = new();
    private NetEntity? _selected;

    protected override void Open()
    {
        base.Open();
        if (State is DropshipTerminalBuiState s)
            Set(s);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is DropshipTerminalBuiState s)
            Set(s);
    }

    private void Set(DropshipTerminalBuiState state)
    {
        _window ??= this.CreateWindow<DropshipTerminalWindow>();
        SetHeader(state.Name);
        FillDropships(state.Dropships);

        _window.SummonButton.Button.OnPressed += _ =>
        {
            SummonDropship();
            SetSummonButtonDisabled(true);
            ResetButtons();
            _selected = null;
        };
    }

    private void SummonDropship()
    {
        if (_window == null || _selected is not { } selected)
            return;

        SendPredictedMessage(new DropshipTerminalSummonDropshipMsg(selected));
    }

    private void SetHeader(string label)
    {
        _window?.Header.SetMarkup($"[color=#0BDC49][font size=16][bold]{label}[/bold][/font][/color]");
    }

    private void SetSummonButtonDisabled(bool disabled)
    {
        if (_window == null)
            return;

        _window.SummonButton.Button.Disabled = disabled;
    }

    private void ResetButtons()
    {
        if (_window == null)
            return;

        foreach (var dropship in _window.DropshipContainer.Children)
        {
            if (dropship is not DropshipButton button ||
                !_dropships.TryGetValue(button, out var name))
            {
                continue;
            }

            button.Text = name;
        }
    }

    private void FillDropships(List<DropshipEntry> dropships)
    {
        if (_window == null)
            return;

        DropshipButton Row(string name, Action onPressed)
        {
            var button = new DropshipButton();
            button.Text = name;
            button.BorderColor = Color.Transparent;
            button.BorderThickness = new Thickness(0);
            button.Button.ToggleMode = false;
            button.Button.OnPressed += _ =>
            {
                ResetButtons();
                SetSummonButtonDisabled(false);
                button.Text = $"> {name}";
                onPressed();
            };
            return button;
        }

        _dropships.Clear();
        _window.DropshipContainer.DisposeAllChildren();
        foreach (var dropship in dropships)
        {
            var button = Row(dropship.Name, () => _selected = dropship.Id);
            _dropships[button] = dropship.Name;
            _window.DropshipContainer.AddChild(button);
        }
    }
}
