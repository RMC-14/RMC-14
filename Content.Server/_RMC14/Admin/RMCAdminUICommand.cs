using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Admin;

[AdminCommand(AdminFlags.Admin)]
public sealed class RMCAdminUICommand : LocalizedCommands
{
    [Dependency] private readonly EuiManager _eui = default!;

    public override string Command => "rmcadminui";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteLine("shell-only-players-can-run-this-command");
            return;
        }

        _eui.OpenEui(new RMCGlobalAdminEui(), player);
    }
}
