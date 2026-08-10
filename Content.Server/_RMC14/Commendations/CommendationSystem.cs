using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared._RMC14.Commendations;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Xenonids.Name;
using Content.Shared.Database;
using Content.Server.Administration;
using Content.Shared.Dataset;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Commendations;

public sealed class CommendationSystem : SharedCommendationSystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly CommendationManager _commendation = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedRankSystem _rank = default!;

    public override async void GiveCommendation(
        Entity<CommendationGiverComponent?, ActorComponent?> giver,
        Entity<CommendationReceiverComponent?> receiver,
        string name,
        string text,
        CommendationType type,
        EntProtoId? commendationPrototypeId = null)
    {
        try
        {
            base.GiveCommendation(giver, receiver, name, text, type, commendationPrototypeId);

            if (!Resolve(giver, ref giver.Comp1, ref giver.Comp2, false) ||
                !Resolve(receiver, ref receiver.Comp, false) ||
                receiver.Comp.LastPlayerId == null)
            {
                return;
            }

            var receiverId = Guid.Parse(receiver.Comp.LastPlayerId);
            var receiverName = GetNameWithRank(receiver);

            await GiveCommendationInternal(giver, receiverId, receiverName, name, text, type, commendationPrototypeId, receiver);
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
        CommendationType type,
        EntProtoId? commendationPrototypeId = null)
    {
        try
        {
            base.GiveCommendationByLastPlayerId(giver, lastPlayerId, receiverName, name, text, type, commendationPrototypeId);

            if (!Resolve(giver, ref giver.Comp1, ref giver.Comp2, false))
                return;

            if (!Guid.TryParse(lastPlayerId, out var receiverId))
                return;

            await GiveCommendationInternal(giver, receiverId, receiverName, name, text, type, commendationPrototypeId, null);
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
        CommendationType type,
        EntProtoId? commendationPrototypeId = null,
        Entity<CommendationReceiverComponent?>? receiver = null)
    {
        text = text.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (CharacterLimit > 0 && text.Length > CharacterLimit)
            text = text[..CharacterLimit];

        if (giver.Comp1 == null || giver.Comp2 == null)
            return;

        var giverId = giver.Comp2.PlayerSession.UserId;
        var giverName = GetNameWithRank(giver);
        var round = _gameTicker.RoundId;

        giver.Comp1.Given++;
        Dirty(giver, giver.Comp1);

        var commendation = new Commendation(giverName, receiverName, name, text, type, round);
        var receiverLastPlayerId = receiverId.ToString();
        NetEntity? receiverEntity = receiver.HasValue ? GetNetEntity(receiver.Value.Owner) : null;
        var entry = new RoundCommendationEntry(commendation, commendationPrototypeId, receiverEntity, receiverLastPlayerId);
        RoundCommendations.Add(entry);
        _commendation.CommendationAdded(giverId, new NetUserId(receiverId), commendation);
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

    public int GetMedalCount()
    {
        return GetAwardableMedalIds().Count + GetSpecialMedalIds().Count;
    }

    public int GetJellyCount()
    {
        var regular = _prototype.Index<LocalizedDatasetPrototype>(JellyDatasetId);
        var special = _prototype.Index<LocalizedDatasetPrototype>(JellySpecialDatasetId);
        return regular.Values.Count + special.Values.Count;
    }

    public bool TryGetAwardInfo(CommendationType type, int awardIndex, out string awardName, out EntProtoId? protoId)
    {
        if (type == CommendationType.Medal)
        {
            var medals = GetAwardableMedalIds();
            var specials = GetSpecialMedalIds();
            var total = medals.Count + specials.Count;

            if (awardIndex < 1 || awardIndex > total)
            {
                awardName = string.Empty;
                protoId = null;
                return false;
            }

            var medalId = awardIndex <= medals.Count
                ? medals[awardIndex - 1]
                : specials[awardIndex - medals.Count - 1];

            protoId = medalId;
            awardName = _prototype.Index<EntityPrototype>(medalId).Name;
            return true;
        }

        if (type == CommendationType.Jelly)
        {
            var regular = _prototype.Index<LocalizedDatasetPrototype>(JellyDatasetId);
            var special = _prototype.Index<LocalizedDatasetPrototype>(JellySpecialDatasetId);
            var regularCount = regular.Values.Count;
            var total = regularCount + special.Values.Count;

            if (awardIndex < 1 || awardIndex > total)
            {
                awardName = string.Empty;
                protoId = null;
                return false;
            }

            var locId = awardIndex <= regularCount
                ? regular.Values[awardIndex - 1]
                : special.Values[awardIndex - regularCount - 1];

            protoId = null;
            awardName = Loc.GetString(locId);
            return true;
        }

        awardName = string.Empty;
        protoId = null;
        return false;
    }

    public async Task<string?> AdminGiveCommendation(
        Guid adminId,
        string adminName,
        string giverName,
        string receiverNameOrId,
        string receiverCharacterName,
        CommendationType type,
        string awardName,
        EntProtoId? protoId,
        string citation,
        int? targetRound = null)
    {
        citation = citation.Trim();
        if (string.IsNullOrWhiteSpace(citation))
            return Loc.GetString("cmd-rmcgivecommendation-empty-citation");

        var located = await _playerLocator.LookupIdByNameOrIdAsync(receiverNameOrId);
        if (located == null)
            return Loc.GetString("cmd-rmcgivecommendation-player-not-found", ("player", receiverNameOrId));

        var receiverId = located.UserId.UserId;

        if (await _db.GetPlayerRecordByUserId(located.UserId) == null)
            return Loc.GetString("cmd-rmcgivecommendation-player-never-joined", ("player", located.Username));

        var currentRound = _gameTicker.RoundId;
        var actualRound = targetRound ?? currentRound;

        if (actualRound < 1 || actualRound > currentRound)
            return Loc.GetString("cmd-rmcgivecommendation-invalid-round", ("round", actualRound), ("current", currentRound));

        try
        {
            await _db.AddCommendation(adminId, receiverId, giverName, receiverCharacterName, awardName, citation, type, actualRound);

            var commendation = new Commendation(giverName, receiverCharacterName, awardName, citation, type, actualRound);
            _commendation.CommendationAdded(new NetUserId(adminId), new NetUserId(receiverId), commendation);

            if (actualRound == currentRound)
            {
                var entry = new RoundCommendationEntry(
                    commendation,
                    protoId,
                    null,
                    // Deliberately not linked to a player entity to avoid confusing admins when the player has changed roles.
                    receiverId.ToString());
                RoundCommendations.Add(entry);
            }

            var typeName = type == CommendationType.Medal ? "medal" : "jelly";
            var receiverLogin = located.Username;

            _adminLog.Add(LogType.RMCMedal,
                $"admin {adminName} gave a {typeName} '{awardName}' to {receiverLogin} (character: {receiverCharacterName}) that reads:\n{citation}");

            _chat.SendAdminAnnouncement(Loc.GetString("cmd-rmcgivecommendation-admin-announcement",
                ("admin", adminName),
                ("type", typeName),
                ("award", awardName),
                ("receiver", receiverLogin),
                ("character", receiverCharacterName),
                ("round", actualRound)));

            return null;
        }
        catch (Exception e)
        {
            Log.Error($"Error in AdminGiveCommendation: {e}");
            return (e.InnerException ?? e).Message;
        }
    }

    /// <summary>
    /// Gets the name with rank/rank prefix for commendations.
    /// </summary>
    private string GetNameWithRank(EntityUid uid)
    {
        if (HasComp<XenoNameComponent>(uid))
            return Name(uid);

        var rankName = _rank.GetSpeakerFullRankName(uid);
        return rankName ?? Name(uid);
    }
}
