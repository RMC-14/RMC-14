using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._RMC14.ERT;
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
