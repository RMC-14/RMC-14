using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.ERT;

public sealed class RMCERTAdminSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _eui = default!;

    private readonly HashSet<RMCERTAdminEui> _open = [];

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCERTStateChangedEvent>(OnERTStateChanged);
    }

    public void Open(IConsoleShell shell)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteLine("This command can only be used by a player.");
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

[AdminCommand(AdminFlags.Admin)]
public sealed class RMCERTAdminCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public override string Command => "rmcert";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _entities.System<RMCERTAdminSystem>().Open(shell);
    }
}
