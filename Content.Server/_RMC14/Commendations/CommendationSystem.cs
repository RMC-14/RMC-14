using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared._RMC14.Commendations;
using Content.Shared.Database;
using Robust.Shared.Network;
using Robust.Shared.Player;
using System.Threading.Tasks;

namespace Content.Server._RMC14.Commendations;

public sealed class CommendationSystem : SharedCommendationSystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly CommendationManager _commendation = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override async void GiveCommendation(
        Entity<CommendationGiverComponent?, ActorComponent?> giver,
        Entity<CommendationReceiverComponent?> receiver,
        string name,
        string text,
        CommendationType type)
    {
        try
        {
            base.GiveCommendation(giver, receiver, name, text, type);

            if (!Resolve(giver, ref giver.Comp1, ref giver.Comp2, false) ||
                !Resolve(receiver, ref receiver.Comp, false) ||
                receiver.Comp.LastPlayerId == null)
            {
                return;
            }

            var receiverId = Guid.Parse(receiver.Comp.LastPlayerId);
            var receiverName = Name(receiver);

            await GiveCommendationInternal(giver, receiverId, receiverName, name, text, type);
        }
        catch (Exception e)
        {
            Log.Error($"Error giving commendation, giver: {giver.Owner}, receiver: {receiver.Owner}\n{e}");
        }
    }

    public override async void GiveCommendationByLastPlayerId(
        Entity<CommendationGiverComponent?, ActorComponent?> giver,
        string lastPlayerId,
        string receiverName,
        string name,
        string text,
        CommendationType type)
    {
        try
        {
            base.GiveCommendationByLastPlayerId(giver, lastPlayerId, receiverName, name, text, type);

            if (!Resolve(giver, ref giver.Comp1, ref giver.Comp2, false))
                return;

            if (!Guid.TryParse(lastPlayerId, out var receiverId))
                return;

            await GiveCommendationInternal(giver, receiverId, receiverName, name, text, type);
        }
        catch (Exception e)
        {
            Log.Error($"Error giving commendation by last player id, giver: {giver.Owner}, lastPlayerId: {lastPlayerId}\n{e}");
        }
    }

    private async Task GiveCommendationInternal(
        Entity<CommendationGiverComponent?, ActorComponent?> giver,
        Guid receiverId,
        string receiverName,
        string name,
        string text,
        CommendationType type)
    {
        text = text.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (CharacterLimit > 0 && text.Length > CharacterLimit)
            text = text[..CharacterLimit];

        if (giver.Comp1 == null || giver.Comp2 == null)
            return;

        var giverId = giver.Comp2.PlayerSession.UserId;
        var giverName = Name(giver);
        var round = _gameTicker.RoundId;

        giver.Comp1.Given++;
        Dirty(giver, giver.Comp1);

        var commendation = new Commendation(giverName, receiverName, name, text, type, round);
        RoundCommendations.Add(commendation);
        _commendation.CommendationAdded(new NetUserId(receiverId), commendation);
        _adminLog.Add(LogType.RMCMedal, $"{ToPrettyString(giver)} gave a medal to {receiverName} of type {type} {name} that reads:\n{text}");

        try
        {
            await _db.AddCommendation(giverId, receiverId, giverName, receiverName, name, text, type, round);
        }
        catch (Exception e)
        {
            Log.Error($"Error saving commendation to database, giver: {giverName}, receiver: {receiverName}, round: {round}:\n{e}");
        }
    }
}
