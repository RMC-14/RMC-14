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
    public required List<MarineAwardRecommendationInfo> Recommendations { get; init; }
}
