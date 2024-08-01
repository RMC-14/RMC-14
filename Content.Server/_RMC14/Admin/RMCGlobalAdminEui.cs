using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared.Administration;
using Content.Shared.Eui;

namespace Content.Server._RMC14.Admin;

public sealed class RMCGlobalAdminEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IEntityManager _entities = default!;

    private readonly SquadSystem _squad;

    public RMCGlobalAdminEui()
    {
        IoCManager.InjectDependencies(this);
        _squad = _entities.System<SquadSystem>();
    }

    public override void Opened()
    {
        _admin.OnPermsChanged += OnAdminPermsChanged;
        StateDirty();
    }

    public override void Closed()
    {
        _admin.OnPermsChanged -= OnAdminPermsChanged;
    }

    public override EuiStateBase GetNewState()
    {
        return RMCAdminEui.CreateState(_entities);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case RMCAdminCreateSquadMsg createSquad:
            {
                _squad.TryEnsureSquad(createSquad.SquadId, out _);
                StateDirty();
                break;
            }
            case RMCAdminRefresh:
            {
                StateDirty();
                break;
            }
        }
    }

    private void OnAdminPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player != Player)
            return;

        if (!_admin.HasAdminFlag(Player, AdminFlags.Admin))
            Close();
    }
}
