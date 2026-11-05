using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Admin.PayloadDeployment;

[AdminCommand(AdminFlags.VarEdit)]
public sealed class RMCPayloadDeploymentUiCommand : LocalizedCommands
{
    [Dependency] private readonly EuiManager _eui = default!;

    public override string Command => "rmcdeploymentui";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        _eui.OpenEui(new RMCPayloadDeploymentEui(), player);
    }
}
