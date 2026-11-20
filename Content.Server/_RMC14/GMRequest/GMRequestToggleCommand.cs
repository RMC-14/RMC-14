using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared._RMC14.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.GMRequest;

[AdminCommand(AdminFlags.Admin)]
public sealed class RMCGMRequestToggleCommand : IConsoleCommand
{
    public string Command => "togglegmrequest";
    public string Description => Loc.GetString("rmc-gm-request-command-toggle-description");
    public string Help => "togglegmrequest";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var cfg = IoCManager.Resolve<IConfigurationManager>();

        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("rmc-gm-request-command-toggle-too-many-arguments-error"));
            return;
        }

        var gmrequest = cfg.GetCVar(RMCCVars.RMCGMRequestEnabled);

        if (args.Length == 0)
        {
            gmrequest = !gmrequest;
        }

        if (args.Length == 1 && !bool.TryParse(args[0], out gmrequest))
        {
            shell.WriteError(Loc.GetString("rmc-gm-request-command-toggle-invalid-argument-error"));
        }

        cfg.SetCVar(RMCCVars.RMCGMRequestEnabled, gmrequest);

        shell.WriteLine(Loc.GetString(gmrequest ? "rmc-gm-request-command-toggle-enabled" : "rmc-gm-request-command-toggle-disabled"));
    }
}
