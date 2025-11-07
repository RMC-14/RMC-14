using System.Linq;
using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Admin.ChatBans;

[AdminCommand(AdminFlags.Ban)]
public sealed class RMCAdminChatBanListCommand : LocalizedCommands
{
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;

    public override string Command => "rmcadminchatbanlist";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        try
        {
            if (shell.Player == null)
                return;

            var data = await _playerLocator.LookupIdByNameOrIdAsync(string.Join(" ", args));
            if (data == null)
            {
                shell.WriteLine(Loc.GetString("rmc-chat-bans-no-player-found", ("player", argStr)));
                return;
            }

            _eui.OpenEui(new RMCAdminChatBansListEui { Target = data.UserId }, shell.Player);
        }
        catch (Exception e)
        {
            _log.GetSawmill("rmc_chat_bans").Error($"Error running {Command}:\n{e}");
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        var options = _player.Sessions.Select(c => c.Name).OrderBy(c => c).ToArray();
        return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-banlist-hint"));
    }
}
