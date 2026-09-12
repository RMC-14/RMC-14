using Content.Shared.Database;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Admin.Commendations;

[Serializable, NetSerializable]
public sealed class RMCAdminGiveCommendationMsg(
    string giverName,
    string receiverNameOrId,
    string receiverCharacterName,
    CommendationType type,
    int awardIndex,
    string citation,
    int? targetRound) : EuiMessageBase
{
    public readonly string GiverName = giverName;
    public readonly string ReceiverNameOrId = receiverNameOrId;
    public readonly string ReceiverCharacterName = receiverCharacterName;
    public readonly CommendationType Type = type;
    public readonly int AwardIndex = awardIndex;
    public readonly string Citation = citation;
    public readonly int? TargetRound = targetRound;
}

[Serializable, NetSerializable]
public sealed class RMCAdminGiveCommendationErrorMsg(string message) : EuiMessageBase
{
    public readonly string Message = message;
}
