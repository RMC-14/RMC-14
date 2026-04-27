using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._RMC14.ERT;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.ERT;

/// <summary>
/// Tracks open admin ERT windows and fans out state changes to them.
/// </summary>
public sealed class RMCERTAdminSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly EuiManager _eui = default!;

    private readonly HashSet<RMCERTAdminEui> _open = [];

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCERTStateChangedEvent>(OnERTStateChanged);
    }

    /// <summary>
    /// Opens the ERT admin window for a player-backed admin shell.
    /// </summary>
    public void Open(IConsoleShell shell)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteLine(Loc.GetString("rmc-ert-admin-command-player-only"));
            return;
        }

        if (!_admin.IsAdmin(player))
        {
            shell.WriteError(Loc.GetString("rmc-ert-admin-command-admin-only"));
            return;
        }

        _eui.OpenEui(new RMCERTAdminEui(), player);
    }

    public void Register(RMCERTAdminEui eui)
    {
        _open.Add(eui);
    }

    public void Unregister(RMCERTAdminEui eui)
    {
        _open.Remove(eui);
    }

    private void OnERTStateChanged(ref RMCERTStateChangedEvent ev)
    {
        foreach (var eui in _open)
        {
            eui.StateDirty();
        }
    }
}

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
