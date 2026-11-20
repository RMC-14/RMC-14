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
    [Dependency] private readonly ILogManager _log = default!;

    private bool _processing;

    public RMCAdminCommendationEui()
    {
        IoCManager.InjectDependencies(this);
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        try
        {
            base.HandleMessage(msg);

            if (IsShutDown)
                return;

            if (!_admin.HasAdminFlag(Player, AdminFlags.Commendations))
            {
                Close();
                return;
            }

            if (msg is not RMCAdminGiveCommendationMsg give || _processing)
                return;

            var cs = _systems.GetEntitySystem<CommendationSystem>();

            if (!cs.TryGetAwardInfo(give.Type, give.AwardId, out var awardName, out var protoId))
            {
                SendMessage(new RMCAdminGiveCommendationErrorMsg(Loc.GetString("rmc-give-commendation-error-invalid-award")));
                return;
            }

            AdminCommendationResult result;
            _processing = true;
            try
            {
                result = await cs.AdminGiveCommendation(
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
            }
            finally
            {
                _processing = false;
            }

            if (IsShutDown)
                return;

            if (result.Error != null)
                SendMessage(new RMCAdminGiveCommendationErrorMsg(result.Error));
            else
                Close();
        }
        catch (Exception e)
        {
            _log.GetSawmill("rmc_admin_commendation").Error($"Error processing message of type {msg.GetType()}:\n{e}");
        }
    }
}
