using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Commendations;
using Content.Shared.Database;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Commendations;

public sealed class CommendationSystem : SharedCommendationSystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly CommendationManager _commendation = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    private int _characterLimit;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_config, RMCCVars.RMCCommendationMaxLength, v => _characterLimit = v, true);
    }

    public override async void GiveCommendation(
        Entity<CommendationGiverComponent?, ActorComponent?> giver,
        Entity<CommendationReceiverComponent?> receiver,
        string name,
        string text,
        CommendationType type)
    {
        base.GiveCommendation(giver, receiver, name, text, type);

        if (!Resolve(giver, ref giver.Comp1, ref giver.Comp2, false) ||
            !Resolve(receiver, ref receiver.Comp, false) ||
            receiver.Comp.LastPlayerId == null)
        {
            return;
        }

        text = text.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (_characterLimit > 0 && text.Length > _characterLimit)
            text = text[.._characterLimit];

        var giverId = giver.Comp2.PlayerSession.UserId;
        var giverName = Name(giver);
        var receiverId = Guid.Parse(receiver.Comp.LastPlayerId);
        var receiverName = Name(receiver);
        var round = _gameTicker.RoundId;

        giver.Comp1.Given++;
        Dirty(giver, giver.Comp1);

        var commendation = new Commendation(giverName, receiverName, name, text, type, round);
        RoundCommendations.Add(commendation);
        _commendation.CommendationAdded(new NetUserId(receiverId), commendation);
        _adminLog.Add(LogType.RMCMedal, $"{ToPrettyString(giver)} gave a medal to {ToPrettyString(receiver)} of type {type} {name} that reads:\n{text}");

        try
        {
            await _db.AddCommendation(giverId, receiverId, giverName, receiverName, name, text, type, round);
        }
        catch (Exception e)
        {
            Log.Error($"Error giving commendation from {receiverName} to {giverName} for round {round}:\n{e}");
        }
    }
}
