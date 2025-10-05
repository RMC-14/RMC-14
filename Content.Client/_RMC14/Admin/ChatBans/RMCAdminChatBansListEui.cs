using Content.Client._RMC14.UserInterface;
using Content.Client.Eui;
using Content.Shared._RMC14.Admin.ChatBans;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Admin.ChatBans;

[UsedImplicitly]
public sealed class RMCAdminChatBansListEui : BaseEui
{
    private RMCAdminChatBanListWindow? _window;
    private RMCAdminChatBanListState? _state;

    public override void Opened()
    {
        base.Opened();

        _window = new RMCAdminChatBanListWindow();
        Refresh();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window?.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);

        if (state is not RMCAdminChatBanListState s)
            return;

        _state = s;
        Refresh();
    }

    private void Refresh()
    {
        if (_window == null || _state == null)
            return;

        _window.Container.DisposeAllChildren();
        foreach (var ban in _state.Bans)
        {
            var row = new RMCAdminChatBanRow();
            row.TypeLabel.Text = ban.Type.ToString();
            row.ReasonLabel.Text = FormattedMessage.EscapeText(ban.Reason);
            row.BannedAtLabel.Text = $"{ban.BannedAt:MM/dd/yyyy h:mm tt}";
            row.ExpiresAtLabel.Text = ban.ExpiresAt == null
                ? "rmc-chat-bans-list-permanent"
                : $"{ban.ExpiresAt:MM/dd/yyyy h:mm tt}";
            if (ban.UnbannedBy != null)
            {
                row.ExpiresAtLabel.Text += $"\n{Loc.GetString("ban-list-unbanned", ("date", $"{ban.UnbannedAt:MM/dd/yyyy h:mm tt}"))}";
                row.ExpiresAtLabel.Text += $"\n{Loc.GetString("ban-list-unbanned-by", ("unbanner", ban.UnbannedBy))}";
                row.PardonButton.Visible = false;
            }

            row.PardonButton.OnPressed += _ => SendMessage(new RMCAdminChatBanListPardonMsg(ban.Id));

            _window.Container.AddChild(row);
            _window.Container.AddChild(new BlueHorizontalSeparator());
        }
    }
}
