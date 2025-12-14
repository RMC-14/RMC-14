using Content.Shared._RMC14.Commendations;
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
    public Dictionary<string, GibbedMarineInfo> GibbedMarines = new();

    [DataField, AutoNetworkedField]
    public List<MarineAwardRecommendationInfo> AwardRecommendations = new();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? LastShipAnnouncement;

    [DataField, AutoNetworkedField]
    public TimeSpan ShipAnnouncementCooldown = TimeSpan.FromSeconds(30);
}

[Serializable, NetSerializable]
public sealed class GibbedMarineInfo
{
    public string Name = string.Empty;
    public string? LastPlayerId;
}

[Serializable, NetSerializable]
public sealed class MarineAwardRecommendationInfo
{
    public string RecommenderName = string.Empty;
    public string RecommenderRank = string.Empty;
    public string RecommenderJob = string.Empty;
    public string RecommendedName = string.Empty;
    public string RecommendedLastPlayerId = string.Empty;
    public string RecommenderLastPlayerId = string.Empty;
    public string Reason = string.Empty;
}
