using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared._RMC14.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Chat.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class SetDchatCommand : IConsoleCommand
{
    public string Command => "setdchat";
    public string Description => Loc.GetString("set-dchat-command-description");
    public string Help => Loc.GetString("set-dchat-command-help");
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var cfg = IoCManager.Resolve<IConfigurationManager>();

        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("set-dchat-command-too-many-arguments-error"));
            return;
        }

        var dchat = cfg.GetCVar(RMCCVars.RMCDeadChatEnabled);

        if (args.Length == 0)
        {
            dchat = !dchat;
        }

        if (args.Length == 1 && !bool.TryParse(args[0], out dchat))
        {
            shell.WriteError(Loc.GetString("set-dchat-command-invalid-argument-error"));
            return;
        }

        cfg.SetCVar(RMCCVars.RMCDeadChatEnabled, dchat);

        shell.WriteLine(Loc.GetString(dchat ? "set-dchat-command-dchat-enabled" : "set-dchat-command-dchat-disabled"));
    }
}
