using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Marines.ControlComputer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedMarineControlComputerSystem))]
public sealed partial class MarineControlComputerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Evacuating;

    [DataField, AutoNetworkedField]
    public bool CanEvacuate;

    // TODO make new sound for this
    // [DataField, AutoNetworkedField]
    // public SoundSpecifier? EvacuationStartSound = new SoundSpecifier("/Audio/_RMC14/Announcements/ARES/evacuation_start.ogg", AudioParams.Default.WithVolume(-5));

    [DataField, AutoNetworkedField]
    public SoundSpecifier? EvacuationCancelledSound = new SoundPathSpecifier("/Audio/_RMC14/Announcements/ARES/evacuate_cancelled.ogg", AudioParams.Default.WithVolume(-5));

    [DataField, AutoNetworkedField]
    public TimeSpan ToggleCooldown = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan LastToggle;

    [DataField, AutoNetworkedField]
    public HashSet<GibbedMarineInfo> GibbedMarines = new();

    [DataField, AutoNetworkedField]
    public HashSet<MarineAwardRecommendationInfo> AwardRecommendations = new();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? LastShipAnnouncement;

    [DataField, AutoNetworkedField]
    public TimeSpan ShipAnnouncementCooldown = TimeSpan.FromSeconds(30);

    [DataField, AutoNetworkedField]
    public bool CanPrintCommendations = true;

    [DataField, AutoNetworkedField]
    public TimeSpan PrintCommendationDelay = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public SoundSpecifier? PrintCommendationSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/print.ogg");

    /// <summary>
    /// List of improvised hash of printed commendations
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> PrintedCommendationIds = new();
}

[Serializable, NetSerializable]
public sealed record GibbedMarineInfo
{
    public required string Name { get; init; }
    public required string LastPlayerId { get; init; }
    public required string Job { get; init; }
    public string? Rank { get; init; }
    public string? Squad { get; init; }
}

[Serializable, NetSerializable]
public sealed class MarineAwardRecommendationInfo : IEquatable<MarineAwardRecommendationInfo>
{
    public required string RecommendedLastPlayerId { get; init; }
    public required string RecommenderLastPlayerId { get; init; }
    public required string Reason { get; init; }
    public string? RecommenderName { get; init; }
    public string? RecommenderRank { get; init; }
    public string? RecommenderSquad { get; init; }
    public string? RecommenderJob { get; init; }
    public string? RecommendedName { get; init; }
    public string? RecommendedRank { get; init; }
    public string? RecommendedSquad { get; init; }
    public string? RecommendedJob { get; init; }
    public bool IsRejected { get; set; } = false;

    public bool Equals(MarineAwardRecommendationInfo? other)
    {
        if (other is null)
            return false;

        return RecommendedLastPlayerId == other.RecommendedLastPlayerId &&
               RecommenderLastPlayerId == other.RecommenderLastPlayerId &&
               Reason == other.Reason;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as MarineAwardRecommendationInfo);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(RecommendedLastPlayerId, RecommenderLastPlayerId, Reason);
    }
}
