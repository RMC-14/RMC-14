using Content.Client.Eui;
using Content.Shared._RMC14.Mentor.ImaginaryFriend;
using JetBrains.Annotations;

namespace Content.Client._RMC14.UserInterface.Mentor;

[UsedImplicitly]
public sealed class BecomeImaginaryFriendEui : BaseEui
{
    private readonly ConfirmationWindow _window;

    public BecomeImaginaryFriendEui()
    {
        _window = new ConfirmationWindow();

        _window.Setup(
            Loc.GetString("rmc-mentor-imaginary-friend-confirmation-title"),
            Loc.GetString("rmc-mentor-imaginary-friend-confirmation-text"),
            Loc.GetString("rmc-mentor-imaginary-friend-confirmation-confirm"),
            Loc.GetString("rmc-mentor-imaginary-friend-confirmation-cancel"),
            Loc.GetString("rmc-mentor-imaginary-friend-confirmation-confirm-default")
        );

        _window.AcceptButton.OnPressed += _ =>
        {
            SendMessage(new BecomeImaginaryFriendMessage(true, false));
            _window.Close();
        };

        _window.DenyButton.OnPressed += _ =>
        {
            SendMessage(new BecomeImaginaryFriendMessage(false, false));
            _window.Close();
        };

        _window.ExtraButton.OnPressed += _ =>
        {
            SendMessage(new BecomeImaginaryFriendMessage(true, true));
            _window.Close();
        };
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }
}
