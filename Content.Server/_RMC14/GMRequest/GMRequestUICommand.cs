using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.GMRequest;

[AdminCommand(AdminFlags.Admin)]
public sealed class GMRequestUICommand : LocalizedCommands
{
    [Dependency] private readonly EuiManager _eui = default!;

    public override string Command => "gmrequestui";
    public override string Description => Loc.GetString("rmc-gm-request-command-ui-description");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteLine(Loc.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        _eui.OpenEui(new GMRequestEui(), player);
    }
}
