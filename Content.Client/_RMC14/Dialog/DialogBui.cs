using Content.Shared._RMC14.Dialog;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Dialog;

public sealed class DialogBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private RMCDialogWindow? _window;

    protected override void Open()
    {
        _window = this.CreateWindow<RMCDialogWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not DialogBuiState s)
            return;

        if (_window is not { IsOpen: true })
            return;

        _window.Title = s.Title;
        _window.Options.DisposeAllChildren();
        for (var i = 0; i < s.Options.Count; i++)
        {
            var option = s.Options[i];
            var button = new Button
            {
                Text = option,
                StyleClasses = { "OpenBoth" },
            };
            button.Label.AddStyleClass("CMAlignLeft");

            var index = i;
            button.OnPressed += _ => SendPredictedMessage(new DialogChosenBuiMsg(index));

            _window.Options.AddChild(button);
        }
    }
}
