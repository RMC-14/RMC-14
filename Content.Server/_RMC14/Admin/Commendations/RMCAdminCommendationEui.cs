using Content.Server._RMC14.Commendations;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._RMC14.Admin.Commendations;
using Content.Shared.Administration;
using Content.Shared.Eui;

namespace Content.Server._RMC14.Admin.Commendations;

public sealed class RMCAdminCommendationEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;

    public RMCAdminCommendationEui()
    {
        IoCManager.InjectDependencies(this);
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (!_admin.HasAdminFlag(Player, AdminFlags.Commendations))
        {
            Close();
            return;
        }

        if (msg is not RMCAdminGiveCommendationMsg give)
            return;

        var cs = _systems.GetEntitySystem<CommendationSystem>();

        if (!cs.TryGetAwardInfo(give.Type, give.AwardIndex, out var awardName, out var protoId))
        {
            SendMessage(new RMCAdminGiveCommendationErrorMsg(Loc.GetString("rmc-give-commendation-error-invalid-award")));
            return;
        }

        var error = await cs.AdminGiveCommendation(
            Player.UserId.UserId,
            Player.Name,
            give.GiverName,
            give.ReceiverNameOrId,
            give.ReceiverCharacterName,
            give.Type,
            awardName,
            protoId,
            give.Citation,
            give.TargetRound);

        if (error != null)
            SendMessage(new RMCAdminGiveCommendationErrorMsg(error));
        else
            Close();
    }
}
