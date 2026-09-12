using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
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
    [Dependency] private readonly IChatManager _chat = default!;
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
        if (!HasAdminAccess())
        {
            Close();
            return;
        }

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
        return _ert.CreateAdminState(CanForceCalls());
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (!HasAdminAccess())
            return;

        // Keep the UI thin: validate permissions here, then hand the actual request mutation off to RMCERTSystem.
        var admin = Player.AttachedEntity;
        switch (msg)
        {
            case RMCERTAdminRefreshMsg:
                StateDirty();
                break;
            case RMCERTAdminApproveRandomMsg approve:
                _ert.ApproveRequest(new RMCERTApproveRequestArgs
                {
                    Request = approve.Request,
                    Actor = admin,
                    ActorName = Player.Name,
                });
                StateDirty();
                break;
            case RMCERTAdminApproveSpecificMsg approve:
                _ert.ApproveRequest(new RMCERTApproveRequestArgs
                {
                    Request = approve.Request,
                    Call = approve.Call,
                    Actor = admin,
                    ActorName = Player.Name,
                });
                StateDirty();
                break;
            case RMCERTAdminDenyMsg deny:
                _ert.DenyRequest(new RMCERTRequestActionArgs
                {
                    Request = deny.Request,
                    Actor = admin,
                    ActorName = Player.Name,
                });
                StateDirty();
                break;
            case RMCERTAdminCancelMsg cancel:
                _ert.CancelRequest(new RMCERTRequestActionArgs
                {
                    Request = cancel.Request,
                    Actor = admin,
                    ActorName = Player.Name,
                });
                StateDirty();
                break;
            case RMCERTAdminLaunchMsg launch:
                _ert.LaunchRequest(new RMCERTRequestActionArgs
                {
                    Request = launch.Request,
                    Actor = admin,
                    ActorName = Player.Name,
                });
                StateDirty();
                break;
            case RMCERTAdminForceCallMsg force:
                if (!CanForceCalls())
                    return;

                var canAdminForceCall = _ert.GetCallOptions(new RMCERTCallQueryArgs
                {
                    EnabledOnly = true,
                    AdminSelectableOnly = true,
                }).Any(c => c.Id == force.Call);
                if (!canAdminForceCall)
                {
                    _chat.DispatchServerMessage(Player, Loc.GetString("rmc-ert-error-call-not-force-callable", ("call", force.Call)));
                    return;
                }

                var result = _ert.ForceCall(new RMCERTForceCallArgs
                {
                    Call = force.Call,
                    Actor = admin,
                    ActorName = Player.Name,
                    Reason = force.Reason,
                });
                if (!result.Success && !string.IsNullOrWhiteSpace(result.Error))
                {
                    _chat.DispatchServerMessage(Player, result.Error);
                }

                StateDirty();
                break;
        }
    }

    private void OnAdminPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player != Player)
            return;

        if (!HasAdminAccess())
            Close();
        else
            StateDirty();
    }

    private bool HasAdminAccess()
    {
        return _admin.IsAdmin(Player);
    }

    private bool CanForceCalls()
    {
        return _admin.HasAdminFlag(Player, AdminFlags.Spawn);
    }
}
