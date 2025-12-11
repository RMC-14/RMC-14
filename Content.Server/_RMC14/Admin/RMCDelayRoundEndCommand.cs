using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Shared._RMC14.CCVar;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Admin;

/// <summary>
/// Delays the round end even after the round end screen.
/// </summary>
[AdminCommand(AdminFlags.Fun | AdminFlags.RMCMaintainer)] //RMC14
public sealed class RMCDelayRoundEndCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override string Command => "rmcdelayroundend";
    public override string Description => "Delay the round end.";
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var currentValue = _cfg.GetCVar(RMCCVars.RMCDelayRoundEnd);
        var chatMsg = "";
        var silent = false;

        if (args.Length >= 3)
        {
            shell.WriteError("Expected 1-2 args got 3 or more");
            return;
        }

        if (args.Length >= 2)
            chatMsg = args[1];

        if (args.Length >= 1)
        {
            if (!bool.TryParse(args[0], out var silentParsed))
            {
                shell.WriteError(Loc.GetString("shell-invalid-bool")+": Silent must be a boolean.");
                return;
            }
            silent = silentParsed;
        }

        _chatManager.SendAdminAnnouncement($"{shell.Player}, has set the round end delay to {!currentValue}");

        _cfg.SetCVar(RMCCVars.RMCDelayRoundEnd, !currentValue);

        if (silent)
            return;

        if (chatMsg == "")
        {
            if (currentValue)
            {
                shell.WriteLine("Round End delay has been removed; you must manually issue a command/action to conclude the round still.");
                chatMsg = "[bold][font size=24][color=red]\nThe round end delay has been disabled!\n[/color][/font][/bold]";
            }
            else
            {
                shell.WriteLine("Round End has been delayed; you must manually issue a command/action to conclude the round now.");
                chatMsg = "[bold][font size=24][color=red]\nThe round end has been delayed!\n[/color][/font][/bold]";
            }
        }

        _chatManager.ChatMessageToAll(ChatChannel.Local, chatMsg, chatMsg, default, false, true);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint("Silent"),
            2 => CompletionResult.FromHint("Message"),
            _ => CompletionResult.Empty,
        };
    }
}
