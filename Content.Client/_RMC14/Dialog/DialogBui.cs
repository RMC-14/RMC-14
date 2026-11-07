using Content.Shared._RMC14.Dialog;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Control;

namespace Content.Client._RMC14.Dialog;

[UsedImplicitly]
public sealed class DialogBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private RMCDialogWindow? _window;

    private DialogSystem? _dialog;
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
                    if (child is not ContainerButton button)
                        continue;

                    // Find label inside button
                    Label? label = null;
                    foreach (var subChild in button.Children)
                    {
                        if (subChild is BoxContainer boxContainer)
                        {
                            foreach (var boxChild in boxContainer.Children)
                            {
                                if (boxChild is Label lbl)
                                {
                                    label = lbl;
                                    break;
                                }
                            }
                        }
                        else if (subChild is Label lbl)
                        {
                            label = lbl;
                            break;
                        }
                    }

                    if (label == null || label.Text == null)
                        continue;

                    button.Visible = label.Text.Contains(args.Text, StringComparison.OrdinalIgnoreCase);
                }
            };

            _window.Container = container;
            _window.AddChild(_window.Container);
        }

        _window.Title = s.Title;
        container.Message.Text = s.Message.Text;
        container.Message.Visible = container.Message.Text?.Length > 0;

        container.Options.DisposeAllChildren();
        var spriteSystem = EntMan.System<SpriteSystem>();

        for (var i = 0; i < s.Options.Count; i++)
        {
            var option = s.Options[i];

            // Create button with icon and text inside
            var button = new ContainerButton
            {
                StyleClasses = { ContainerButton.StyleClassButton, "OpenBoth" },
                HorizontalExpand = true
            };

            var contentContainer = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalExpand = true
            };

            if (option.Icon != null)
            {
                var iconRect = new TextureRect
                {
                    SetWidth = 32,
                    SetHeight = 32,
                    HorizontalAlignment = HAlignment.Left,
                    VerticalAlignment = VAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0),
                    Texture = spriteSystem.Frame0(option.Icon)
                };
                contentContainer.AddChild(iconRect);
            }

            var label = new Label
            {
                Text = option.Text,
                HorizontalExpand = true,
                VerticalAlignment = VAlignment.Center
            };
            label.AddStyleClass("CMAlignLeft");
            contentContainer.AddChild(label);

            button.AddChild(contentContainer);

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
            container.MessageLineEdit.OnTextChanged += args => OnInputTextChanged(container, args.Text, s.MinCharacterLimit, s.CharacterLimit, s.SmartCheck);
            container.MessageTextEdit.OnTextChanged += args => OnInputTextChanged(container, Rope.Collapse(args.TextRope), s.MinCharacterLimit, s.CharacterLimit, s.SmartCheck);
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
            OnInputTextChanged(container, string.Empty, s.MinCharacterLimit, s.CharacterLimit, s.SmartCheck);
        }

        _window.Title = string.Empty;
        container.MessageLabel.Text = s.Message.Text;
        container.MessageLineEdit.Visible = !s.LargeInput;
        container.MessageTextEdit.Visible = s.LargeInput;

        if (container.MessageTextEdit.Visible)
            container.MessageLabel.MaxWidth = 500;

        // Set placeholders based on SmartCheck
        if (s.SmartCheck)
        {
            container.MessageLineEdit.PlaceHolder = Loc.GetString("rmc-dialog-input-placeholder-smart-check");
            container.MessageTextEdit.Placeholder = new Rope.Leaf(Loc.GetString("rmc-dialog-input-placeholder-smart-check"));
        }
        else
        {
            container.MessageLineEdit.PlaceHolder = Loc.GetString("rmc-dialog-input-placeholder-default");
            container.MessageTextEdit.Placeholder = new Rope.Leaf(Loc.GetString("rmc-dialog-input-placeholder-default"));
        }

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

    private void OnInputTextChanged(RMCDialogInputContainer container, string text, int min, int max, bool smartCheck)
    {
        _dialog ??= EntMan.System<DialogSystem>();
        var textLength = _dialog.CalculateEffectiveLength(text, smartCheck);
        container.CharacterCount.Text = min > 0
            ? $"{textLength} / {min}-{max}"
            : $"{textLength} / {max}";
        container.OkButton.Disabled = textLength > max || textLength < min;
    }
}
