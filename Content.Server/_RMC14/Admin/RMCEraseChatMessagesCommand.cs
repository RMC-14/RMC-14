using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Admin;

[AdminCommand(AdminFlags.Admin)]
public sealed class RMCEraseChatMessagesCommand : LocalizedCommands
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPlayerLocator _locator = default!;

    public override string Command => "rmcerasechatmessages";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        try
        {
            if (args.Length != 1)
            {
                shell.WriteLine($"Invalid amount of arguments. Usage: {Help}");
                return;
            }

            var name = args[0];
            if (await _locator.LookupIdByNameOrIdAsync(name) is not { } player)
            {
                shell.WriteLine($"No player found with name or id {name}");
                return;
            }

            _chat.DeleteMessagesBy(player.UserId);
            _adminLog.Add(LogType.AdminCommands, $"{shell.Player?.Name:admin} deleted chat messages by {player.Username}");
        }
        catch (Exception e)
        {
            shell.WriteLine($"An error occurred:\n{e}");
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), "player");
    }
}
