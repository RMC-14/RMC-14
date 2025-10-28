using Content.Server.Administration;
using Content.Shared._RMC14.Power;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Admin;

[AdminCommand(AdminFlags.Admin)]
public sealed class RMCRecalculatePower : LocalizedEntityCommands
{
    [Dependency] private readonly SharedRMCPowerSystem _power = default!;

    public override string Command => "rmcrecalculatepower";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _power.RecalculatePower();
    }
}
