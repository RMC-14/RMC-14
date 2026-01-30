using Content.Shared._RMC14.Dialog;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Dialog;

[UsedImplicitly]
public sealed class DialogBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private RMCDialogWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCDialogWindow>();
        Refresh();
    }

    private void UpdateOptions(DialogComponent s)
    {
        if (_window is not { IsOpen: true })
            return;

        if (_window.Container is not RMCDialogOptionsContainer container)
        {
            _window.Container?.Orphan();
            _window.Container = null;

            container = new RMCDialogOptionsContainer();
            container.Search.OnTextChanged += args =>
            {
                foreach (var child in container.Options.Children)
                {
                    if (child is not Button { Text: not null } button)
                        continue;

                    button.Visible = button.Text.Contains(args.Text, StringComparison.OrdinalIgnoreCase);
                }
            };

            _window.Container = container;
            _window.AddChild(_window.Container);
        }

        _window.Title = s.Title;
        container.Message.Text = s.Message.Text;
        container.Message.Visible = container.Message.Text?.Length > 0;

        container.Options.DisposeAllChildren();
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

            container.Options.AddChild(button);
        }
    }

    private void UpdateInput(DialogComponent s)
    {
        if (_window is not { IsOpen: true })
            return;

        if (_window.Container is not RMCDialogInputContainer container)
        {
            _window.Container?.Orphan();
            _window.Container = null;

            container = new RMCDialogInputContainer();
            container.MessageLineEdit.OnTextEntered += args => SendPredictedMessage(new DialogInputBuiMsg(args.Text));
            container.MessageLineEdit.OnTextChanged += args => OnInputTextChanged(container, args.Text.Length, s.CharacterLimit);
            container.MessageTextEdit.OnTextChanged += args => OnInputTextChanged(container, (int) Rope.CalcTotalLength(args.TextRope), s.CharacterLimit);
            container.CancelButton.OnPressed += _ => Close();
            container.OkButton.OnPressed += _ =>
            {
                var msg = EntMan.GetComponentOrNull<DialogComponent>(Owner)?.LargeInput == true
                    ? Rope.Collapse(container.MessageTextEdit.TextRope)
                    : container.MessageLineEdit.Text;
                SendPredictedMessage(new DialogInputBuiMsg(msg));
            };

            _window.Container = container;
            _window.AddChild(_window.Container);
            OnInputTextChanged(container, 0, s.CharacterLimit);
        }

        _window.Title = string.Empty;
        container.MessageLabel.Text = s.Message.Text;
        container.MessageLineEdit.Visible = !s.LargeInput;
        container.MessageTextEdit.Visible = s.LargeInput;

        // Activate input field if AutoFocus is enabled
        if (s.AutoFocus)
        {
            if (!s.LargeInput)
                container.MessageLineEdit.GrabKeyboardFocus();
            else
                container.MessageTextEdit.GrabKeyboardFocus();
        }
    }

    private void UpdateConfirm(DialogComponent s)
    {
        if (_window is not { IsOpen: true })
            return;

        if (_window.Container is not RMCDialogConfirmContainer container)
        {
            _window.Container?.Orphan();
            _window.Container = null;

            container = new RMCDialogConfirmContainer();
            container.CancelButton.OnPressed += _ => Close();
            container.OkButton.OnPressed += _ => SendPredictedMessage(new DialogConfirmBuiMsg());

            _window.Container = container;
            _window.AddChild(_window.Container);
        }

        _window.Title = s.Title;
        container.MessageLabel.Text = s.Message.Text;
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

        _window?.OpenCentered();
    }

    private void OnInputTextChanged(RMCDialogInputContainer container, int textLength, int max)
    {
        container.CharacterCount.Text = $"{textLength} / {max}";
        container.OkButton.Disabled = textLength > max;
    }
}
