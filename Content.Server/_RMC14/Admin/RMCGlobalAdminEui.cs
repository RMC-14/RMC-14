using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Shared.NPC.Systems;

namespace Content.Server._RMC14.Admin;

public sealed class RMCGlobalAdminEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IEntityManager _entities = default!;

    private readonly SquadSystem _squad;
    private readonly NpcFactionSystem _faction;

    private Guid _tacticalMapLines;

    public RMCGlobalAdminEui()
    {
        IoCManager.InjectDependencies(this);
        _squad = _entities.System<SquadSystem>();
        _faction = _entities.System<NpcFactionSystem>();
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
        return RMCAdminEui.CreateState(_entities, _tacticalMapLines);
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
            case RMCAdminRequestTacticalMapHistory history:
            {
                _tacticalMapLines = history.Id;
                StateDirty();
                break;
            }
            case RMCAdminFactionMsg factionChange:
            {
                switch (factionChange.Type)
                {
                    case RMCAdminFactionMsgType.Friendly:
                    {
                        _faction.RealMakeFriendly(factionChange.Left, factionChange.Right);
                        break;
                    }
                    case RMCAdminFactionMsgType.Neutral:
                    {
                        _faction.RealMakeNeutral(factionChange.Left, factionChange.Right);
                        break;
                    }
                    case RMCAdminFactionMsgType.Hostile:
                    {
                        _faction.RealMakeHostile(factionChange.Left, factionChange.Right);
                        break;
                    }
                }
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
