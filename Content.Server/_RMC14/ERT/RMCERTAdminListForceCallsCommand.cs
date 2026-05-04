using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.ERT;

[AdminCommand(AdminFlags.Spawn)]
public sealed class RMCERTAdminListForceCallsCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public override string Command => "rmcertcalls";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var calls = _entities.System<RMCERTSystem>().GetForceCallOptions();
        if (calls.Count == 0)
        {
            shell.WriteLine(Loc.GetString("rmc-ert-admin-command-force-list-empty"));
            return;
        }

        foreach (var call in calls)
        {
            shell.WriteLine(Loc.GetString("rmc-ert-admin-command-force-list-entry",
                ("id", call.Id),
                ("name", call.Name),
                ("category", call.Category),
                ("organization", call.Organization)));
        }
    }
}
