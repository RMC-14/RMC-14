using Content.Shared._RMC14.Dialog;
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
public sealed class MarineMedalsPanelBuiState : BoundUserInterfaceState
{
    public readonly List<MarineRecommendationGroup> RecommendationGroups;

    public MarineMedalsPanelBuiState(List<MarineRecommendationGroup> recommendationGroups)
    {
        RecommendationGroups = recommendationGroups;
    }
}

[Serializable, NetSerializable]
public sealed record MarineRecommendationGroup
{
    public required string LastPlayerId { get; init; }
    public required string Name { get; init; }
    public string? Rank { get; init; }
    public string? Squad { get; init; }
    public required string Job { get; init; }
    public required List<MarineRecommendationInfo> Recommendations { get; init; }
}

[Serializable, NetSerializable]
public sealed record MarineRecommendationInfo
{
    public required string RecommenderLastPlayerId { get; init; }
    public required string RecommenderName { get; init; }
    public string? RecommenderRank { get; init; }
    public string? RecommenderSquad { get; init; }
    public required string RecommenderJob { get; init; }
    public required string Reason { get; init; }
}
