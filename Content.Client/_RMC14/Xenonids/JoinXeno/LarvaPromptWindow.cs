using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Xenonids.JoinXeno;

public sealed class LarvaPromptWindow : DefaultWindow
{
    private readonly Label _timeLabel;
    private readonly Button _acceptButton;
    private readonly Button _declineButton;
    private float _remainingTime;

    public event System.Action? AcceptButtonPressed;
    public event System.Action? DeclineButtonPressed;

    public LarvaPromptWindow(TimeSpan timeout)
    {
        Title = Loc.GetString("rmc-xeno-larva-prompt-title");
        _remainingTime = (float)timeout.TotalSeconds;

        SetSize = new Vector2(400, 200);
        MinSize = new Vector2(400, 200);

        var vBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(10),
        };

        var titleLabel = new RichTextLabel
        {
            SetHeight = 60,
        };
        titleLabel.SetMessage(FormattedMessage.FromMarkupPermissive(Loc.GetString("rmc-xeno-larva-prompt-text")));
        vBox.AddChild(titleLabel);

        _timeLabel = new Label
        {
            Text = Loc.GetString("rmc-xeno-larva-prompt-time", ("seconds", (int)_remainingTime)),
            HorizontalAlignment = Control.HAlignment.Center,
            Margin = new Thickness(0, 10),
        };
        vBox.AddChild(_timeLabel);

        var buttonContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalAlignment = Control.HAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0),
        };

        _acceptButton = new Button
        {
            Text = Loc.GetString("rmc-xeno-larva-prompt-accept"),
            MinSize = new Vector2(100, 40),
            Margin = new Thickness(0, 0, 10, 0),
        };
        _acceptButton.OnPressed += _ => AcceptButtonPressed?.Invoke();
        buttonContainer.AddChild(_acceptButton);

        _declineButton = new Button
        {
            Text = Loc.GetString("rmc-xeno-larva-prompt-decline"),
            MinSize = new Vector2(100, 40),
        };
        _declineButton.OnPressed += _ => DeclineButtonPressed?.Invoke();
        buttonContainer.AddChild(_declineButton);

        vBox.AddChild(buttonContainer);
        Contents.AddChild(vBox);

        OpenCentered();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_remainingTime > 0.0f)
        {
            if (_remainingTime - args.DeltaSeconds < 0)
                _remainingTime = 0;
            else
                _remainingTime -= args.DeltaSeconds;

            var secondsLeft = Math.Max(0, (int)_remainingTime);
            _timeLabel.Text = Loc.GetString("rmc-xeno-larva-prompt-time", ("seconds", secondsLeft));

            if (_remainingTime <= 5.0f)
            {
                _acceptButton.Modulate = Color.FromHex("#FF6B6B");
                _declineButton.Modulate = Color.FromHex("#FF6B6B");
            }
            else if (_remainingTime <= 10.0f)
            {
                _acceptButton.Modulate = Color.FromHex("#FFD93D");
                _declineButton.Modulate = Color.FromHex("#FFD93D");
            }

            if (_remainingTime <= 0.0f)
            {
                Timer.Spawn(0, Close); //better way to do this i just dont know it
            }
        }
    }
}
