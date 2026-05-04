using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.ERT;

[AdminCommand(AdminFlags.Spawn)]
public sealed class RMCERTAdminForceCallCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public override string Command => "rmcertcall";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteError(Loc.GetString("rmc-ert-admin-command-force-usage"));
            return;
        }

        var reason = args.Length > 1
            ? string.Join(' ', args.Skip(1))
            : string.Empty;

        var admin = shell.Player?.AttachedEntity;
        var adminName = shell.Player?.Name;
        var ert = _entities.System<RMCERTSystem>();
        if (!ert.ForceCall(args[0], admin, adminName, reason, out var requestId, out var error))
        {
            shell.WriteError(error);
            return;
        }

        shell.WriteLine(Loc.GetString("rmc-ert-admin-command-force-success",
            ("id", requestId),
            ("call", args[0])));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _entities.System<RMCERTSystem>()
                .GetForceCallOptions()
                .Select(c => c.Id);
            return CompletionResult.FromHintOptions(options, Loc.GetString("rmc-ert-admin-command-force-call-hint"));
        }

        return CompletionResult.FromHint(Loc.GetString("rmc-ert-admin-command-force-reason-hint"));
    }
}
