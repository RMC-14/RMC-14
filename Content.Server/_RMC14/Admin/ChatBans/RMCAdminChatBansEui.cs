using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.Admin.ChatBans;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Eui;

namespace Content.Server._RMC14.Admin.ChatBans;

public sealed class RMCAdminChatBansEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly RMCChatBansManager _rmcChatBans = default!;

    public RMCAdminChatBansEui()
    {
        IoCManager.InjectDependencies(this);
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        try
        {
            base.HandleMessage(msg);
            if (!_admin.HasAdminFlag(Player, AdminFlags.Ban))
            {
                Close();
                return;
            }

            switch (msg)
            {
                case RMCAdminChatBanAddMsg add:
                    var reason = add.Reason;
                    if (reason.Length > 4000)
                        reason = reason[..4000];

                    if (add.Type == ChatType.None)
                    {
                        var response = new RMCAdminChatBanAddErrorMsg(Loc.GetString("rmc-chat-bans-no-player-found", ("target", add.Target)));
                        SendMessage(response);
                        break;
                    }

                    var data = await _playerLocator.LookupIdByNameOrIdAsync(add.Target);
                    if (data == null)
                    {
                        var response = new RMCAdminChatBanAddErrorMsg(Loc.GetString("rmc-chat-bans-no-type-specified"));
                        SendMessage(response);
                        break;
                    }

                    _rmcChatBans.AddChatBan(data.UserId, add.Duration, add.Type, Player.UserId, reason);
                    _chat.SendAdminAlert(Loc.GetString("rmc-chat-bans-added-admin-alert",
                        ("admin", Player.Name),
                        ("target", data.Username),
                        ("type", add.Type),
                        ("date", $"{DateTime.Now + add.Duration:yyyy/MM/dd HH:mm:ss}"),
                        ("reason", reason)));
                    Close();
                    break;
            }
        }
        catch (Exception e)
        {
            _log.GetSawmill("rmc_admin_chat_bans").Error($"Error processing message of type {msg.GetType()}:\n{e}");
        }
    }
}
