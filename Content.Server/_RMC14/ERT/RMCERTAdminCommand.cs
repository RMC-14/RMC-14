using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.ERT;

[AdminCommand(AdminFlags.None)]
public sealed class RMCERTAdminCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public override string Command => "rmcert";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _entities.System<RMCERTAdminSystem>().Open(shell);
    }
}
