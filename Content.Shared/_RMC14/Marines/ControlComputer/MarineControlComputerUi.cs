using Content.Shared._RMC14.Commendations;
using Content.Shared._RMC14.Dialog;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.ControlComputer;

[Serializable, NetSerializable]
public enum MarineControlComputerUi
{
    Key,
    MedalsPanel,
}

[Serializable, NetSerializable]
public sealed class MarineControlComputerAlertLevelMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MarineControlComputerShipAnnouncementMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed record MarineControlComputerShipAnnouncementDialogEvent(NetEntity User, string Message = "") : DialogInputEvent(Message);

[Serializable, NetSerializable]
public sealed class MarineControlComputerMedalMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MarineControlComputerToggleEvacuationMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MarineControlComputerOpenMedalsPanelMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MarineControlComputerApproveRecommendationMsg : BoundUserInterfaceMessage
{
    public required string LastPlayerId { get; init; }
}

[Serializable, NetSerializable]
public sealed class MarineControlComputerRejectRecommendationMsg : BoundUserInterfaceMessage
{
    public required string LastPlayerId { get; init; }
}

[Serializable, NetSerializable]
public sealed class MarineControlComputerRemoveRecommendationGroupMsg : BoundUserInterfaceMessage
{
    public required string LastPlayerId { get; init; }
}

[Serializable, NetSerializable]
public sealed class MarineControlComputerAddMedalMsg : BoundUserInterfaceMessage
{
    public required RoundCommendationEntry MedalEntry { get; init; }
    public required bool CanPrint { get; init; }
    public required bool IsPrinted { get; init; }
}

[Serializable, NetSerializable]
public sealed class MarineControlComputerPrintCommendationMsg : BoundUserInterfaceMessage
{
    public required string CommendationId { get; init; }
}

[Serializable, NetSerializable]
public sealed class MarineMedalsPanelBuiState : BoundUserInterfaceState
{
    public readonly List<MarineRecommendationGroup> RecommendationGroups;
    public readonly List<RoundCommendationEntry> AwardedMedals;
    public readonly bool CanPrintCommendations;
    public readonly HashSet<string> PrintedCommendationIds;

    public MarineMedalsPanelBuiState(List<MarineRecommendationGroup> recommendationGroups, List<RoundCommendationEntry> awardedMedals, bool canPrintCommendations, HashSet<string> printedCommendationIds)
    {
        RecommendationGroups = recommendationGroups;
        AwardedMedals = awardedMedals;
        CanPrintCommendations = canPrintCommendations;
        PrintedCommendationIds = printedCommendationIds;
    }
}

[Serializable, NetSerializable]
public sealed record MarineRecommendationGroup
{
    public required string LastPlayerId { get; init; }
    public required List<MarineAwardRecommendationInfo> Recommendations { get; init; }
}
