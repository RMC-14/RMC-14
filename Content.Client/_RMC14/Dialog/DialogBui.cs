using Content.Shared._RMC14.Dialog;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._RMC14.Dialog;

[UsedImplicitly]
public sealed class DialogBui : BoundUserInterface
{
    private DefaultWindow? _window;

    public DialogBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        Refresh();
    }

    private void UpdateOptions(DialogComponent s)
    {
        if (_window is not RMCDialogOptionsWindow)
        {
            _window?.Close();
            _window = null;
        }

        _window ??= this.CreateWindow<RMCDialogOptionsWindow>();
        if (!_window.IsOpen)
            _window.OpenCentered();

        if (_window is not RMCDialogOptionsWindow { IsOpen: true } window)
            return;

        window.Title = s.Title;
        window.Message.Text = s.Message.Text;
        window.Message.Visible = window.Message.Text?.Length > 0;

        window.Options.DisposeAllChildren();
        for (var i = 0; i < s.Options.Count; i++)
        {
            var option = s.Options[i];
            var button = new Button
            {
                Text = option.Text,
                StyleClasses = { "OpenBoth" },
            };
            button.Label.AddStyleClass("CMAlignLeft");

            var index = i;
            button.OnPressed += _ => SendPredictedMessage(new DialogOptionBuiMsg(index));

            window.Options.AddChild(button);
        }
    }

    private void UpdateInput(DialogComponent s)
    {
        if (_window is not RMCDialogInputWindow)
        {
            _window?.Close();
            _window = null;
        }

        _window ??= this.CreateWindow<RMCDialogInputWindow>();
        if (!_window.IsOpen)
            _window.OpenCentered();

        if (_window is not RMCDialogInputWindow { IsOpen: true } window)
            return;

        window.Title = string.Empty;
        window.MessageLabel.Text = s.Message.Text;
        window.MessageLineEdit.OnTextEntered += args => SendPredictedMessage(new DialogInputBuiMsg(args.Text));
        window.CancelButton.OnPressed += _ => Close();
        window.OkButton.OnPressed += _ => SendPredictedMessage(new DialogInputBuiMsg(window.MessageLineEdit.Text));
    }

    private void UpdateConfirm(DialogComponent s)
    {
        if (_window is not RMCDialogConfirmWindow)
        {
            _window?.Close();
            _window = null;
        }

        _window ??= this.CreateWindow<RMCDialogConfirmWindow>();
        if (!_window.IsOpen)
            _window.OpenCentered();

        if (_window is not RMCDialogConfirmWindow { IsOpen: true } window)
            return;

        window.Title = s.Title;
        window.MessageLabel.Text = s.Message.Text;
        window.CancelButton.OnPressed += _ => Close();
        window.OkButton.OnPressed += _ => SendPredictedMessage(new DialogConfirmBuiMsg());
    }

    public void Refresh()
    {
        if (!EntMan.TryGetComponent(Owner, out DialogComponent? dialog))
            return;

        switch (dialog.DialogType)
        {
            case DialogType.Options:
            {
                UpdateOptions(dialog);
                break;
            }
            case DialogType.Input:
            {
                UpdateInput(dialog);
                break;
            }
            case DialogType.Confirm:
            {
                UpdateConfirm(dialog);
                break;
            }
        }
    }
}
