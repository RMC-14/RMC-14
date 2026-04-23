using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._RMC14.ERT;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Shared.Player;

namespace Content.Server._RMC14.ERT;

/// <summary>
/// Server-side EUI controller for the admin ERT dispatch window.
/// </summary>
public sealed class RMCERTAdminEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IEntityManager _entities = default!;

    private readonly RMCERTAdminSystem _adminSystem;
    private readonly RMCERTSystem _ert;

    public RMCERTAdminEui()
    {
        IoCManager.InjectDependencies(this);
        _adminSystem = _entities.System<RMCERTAdminSystem>();
        _ert = _entities.System<RMCERTSystem>();
    }

    public override void Opened()
    {
        _admin.OnPermsChanged += OnAdminPermsChanged;
        _adminSystem.Register(this);
        StateDirty();
    }

    public override void Closed()
    {
        _admin.OnPermsChanged -= OnAdminPermsChanged;
        _adminSystem.Unregister(this);
    }

    public override EuiStateBase GetNewState()
    {
        return _ert.CreateAdminState();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (!HasPermission())
            return;

        // Keep the UI thin: validate permissions here, then hand the actual request mutation off to RMCERTSystem.
        var admin = Player.AttachedEntity;
        switch (msg)
        {
            case RMCERTAdminRefreshMsg:
                StateDirty();
                break;
            case RMCERTAdminApproveRandomMsg approve:
                _ert.ApproveRandom(approve.Request, admin);
                StateDirty();
                break;
            case RMCERTAdminApproveSpecificMsg approve:
                _ert.ApproveSpecific(approve.Request, approve.Call, admin);
                StateDirty();
                break;
            case RMCERTAdminDenyMsg deny:
                _ert.Deny(deny.Request, admin);
                StateDirty();
                break;
            case RMCERTAdminCancelMsg cancel:
                _ert.Cancel(cancel.Request, admin);
                StateDirty();
                break;
            case RMCERTAdminLaunchMsg launch:
                _ert.Launch(launch.Request, admin);
                StateDirty();
                break;
            case RMCERTAdminCompleteMsg complete:
                _ert.Complete(complete.Request, admin);
                StateDirty();
                break;
        }
    }

    private void OnAdminPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player != Player)
            return;

        if (!HasPermission())
            Close();
    }

    private bool HasPermission()
    {
        return _admin.HasAdminFlag(Player, AdminFlags.Admin);
    }
}
