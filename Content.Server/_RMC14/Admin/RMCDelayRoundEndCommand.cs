using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Shared._RMC14.CCVar;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Admin;

/// <summary>
/// Delays the round from ending via the shuttle call. Can still be ended via other means.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class RMCDelayRoundEndCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override string Command => "rmcdelayroundend";
    public override string Description => "Delay the round end while still allowing the end of round screen to show up";


    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var currentValue = _cfg.GetCVar(RMCCVars.RMCDelayRoundEnd);
        var chatMsg = "";
        var silent = false;

        if (args.Length == 3)
            chatMsg = args[2];

        if (args.Length >= 2)
        {
            if (!bool.TryParse(args[1], out var silentParsed))
            {
                shell.WriteError(Loc.GetString("shell-invalid-bool")+": Silent must be a boolean.");
                return;
            }
            silent = silentParsed;
        }

        if (args.Length >= 1)
        {
            if (!bool.TryParse(args[0], out var value))
            {
                shell.WriteError(Loc.GetString("shell-invalid-bool")+": value must be a boolean.");
                return;
            }

            if (value == currentValue)
            {
                shell.WriteLine($"Value is already {value}");
                return;
            }
        }

        _cfg.SetCVar(RMCCVars.RMCDelayRoundEnd, !currentValue);

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

        if (silent)
            return;

        _chatManager.ChatMessageToAll(ChatChannel.Local, chatMsg, chatMsg, default, false, true);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint("Value"),
            2 => CompletionResult.FromHint("Silent"),
            3 => CompletionResult.FromHint("Message"),
            _ => CompletionResult.Empty,
        };
    }
}
