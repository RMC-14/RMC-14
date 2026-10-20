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
        var canAdminForceCall = ert.GetCallOptions(new RMCERTCallQueryArgs
        {
            EnabledOnly = true,
            AdminSelectableOnly = true,
        }).Any(c => c.Id == args[0]);
        if (!canAdminForceCall)
        {
            shell.WriteError(Loc.GetString("rmc-ert-error-call-not-force-callable", ("call", args[0])));
            return;
        }

        var result = ert.ForceCall(new RMCERTForceCallArgs
        {
            Call = args[0],
            Actor = admin,
            ActorName = adminName,
            Reason = reason,
        });
        if (!result.Success)
        {
            shell.WriteError(result.Error);
            return;
        }

        shell.WriteLine(Loc.GetString("rmc-ert-admin-command-force-success",
            ("id", result.RequestId),
            ("call", args[0])));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _entities.System<RMCERTSystem>()
                .GetCallOptions(new RMCERTCallQueryArgs
                {
                    EnabledOnly = true,
                    AdminSelectableOnly = true,
                })
                .Select(c => c.Id);
            return CompletionResult.FromHintOptions(options, Loc.GetString("rmc-ert-admin-command-force-call-hint"));
        }

        return CompletionResult.FromHint(Loc.GetString("rmc-ert-admin-command-force-reason-hint"));
    }
}
