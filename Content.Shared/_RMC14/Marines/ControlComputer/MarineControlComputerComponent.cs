using Content.Shared._RMC14.Commendations;
using System.Collections.Generic;
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
}

[Serializable, NetSerializable]
public sealed record GibbedMarineInfo
{
    public required string Name { get; init; }

    public string? LastPlayerId { get; init; }

    public string? Rank { get; init; }

    public required string Job { get; init; }

    public string? Squad { get; init; }
}

[Serializable, NetSerializable]
public sealed record MarineAwardRecommendationInfo
{
    public required string RecommenderName { get; init; }
    public required string RecommenderRank { get; init; }
    public required string RecommenderJob { get; init; }
    public required string RecommendedName { get; init; }
    public required string RecommendedLastPlayerId { get; init; }
    public required string RecommenderLastPlayerId { get; init; }
    public required string Reason { get; init; }
}
