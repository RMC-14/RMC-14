using Content.Client.Eui;
using Content.Shared._RMC14.Admin.ChatBans;
using Content.Shared.Database;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Admin.ChatBans;

[UsedImplicitly]
public sealed class RMCAdminChatBansEui : BaseEui
{
    private RMCAdminChatBanWindow? _window;

    public override void Opened()
    {
        base.Opened();
        _window = new RMCAdminChatBanWindow();
        _window.ReasonEdit.Placeholder = new Rope.Leaf(Loc.GetString("rmc-chat-bans-reason-placeholder"));
        _window.SubmitButton.OnPressed += _ =>
        {
            var type = ChatType.None;
            if (_window.DeadButton.Pressed)
                type |= ChatType.Dead;

            if (_window.LoocButton.Pressed)
                type |= ChatType.Looc;

            if (_window.OocButton.Pressed)
                type |= ChatType.Ooc;

            if (!double.TryParse(_window.TimeEdit.Text, out var time))
                time = 0;

            time *= _window.Multiplier;
            var duration = TimeSpan.FromMinutes(time);
            var reason = Rope.Collapse(_window.ReasonEdit.TextRope);
            var msg = new RMCAdminChatBanAddMsg(_window.PlayerEdit.Text, type, duration, reason);
            SendMessage(msg);
        };

        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window?.Close();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (_window == null)
            return;

        _window.ErrorLabel.Text = msg switch
        {
            RMCAdminChatBanAddErrorMsg error => error.Message,
            _ => _window.ErrorLabel.Text,
        };
    }
}
