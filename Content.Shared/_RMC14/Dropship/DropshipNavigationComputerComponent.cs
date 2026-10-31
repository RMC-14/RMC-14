using Content.Shared.Doors.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Timing;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Dropship;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedDropshipSystem))]
public sealed partial class DropshipNavigationComputerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillPilot";

    [DataField, AutoNetworkedField]
    public int MultiplierSkillLevel = 2;

    [DataField, AutoNetworkedField]
    public int FlyBySkillLevel = 2;

    [DataField, AutoNetworkedField]
    public float SkillFlyByMultiplier = 1.5f;

    [DataField, AutoNetworkedField]
    public float SkillTravelMultiplier = 0.5f;

    [DataField, AutoNetworkedField]
    public float SkillRechargeMultiplier = 0.75f;

    [DataField, AutoNetworkedField]
    public bool Hijackable = true;

    [DataField, AutoNetworkedField]
    public EntProtoId<IFFFactionComponent> Faction = "FactionMarine";

    [DataField, AutoNetworkedField]
    public TimeSpan LockoutDuration = TimeSpan.FromMinutes(10);

    [DataField, AutoNetworkedField]
    public TimeSpan LockedOutUntil = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public bool LaunchAlarmStatus;

    [DataField]
    public SoundSpecifier? LaunchAlarmForcedShutdownSound = new SoundPathSpecifier("/Audio/_RMC14/Structures/metalhit.ogg");

    /// <summary>
    /// Title shown on the client navigation window.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string NavigationWindowTitle = "Flight Computer";

    /// <summary>
    /// Whether the current-location fly-by destination should be shown.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowFlyByDestination = true;

    /// <summary>
    /// Whether primary destinations should receive the primary suffix.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowPrimaryDestinationLabel = true;

    /// <summary>
    /// Whether individual aft/port/starboard door controls should be shown.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowIndividualDoorControls = true;

    /// <summary>
    /// Whether launch alarm controls should be shown.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowLaunchAlarmControls = true;

    [DataField, AutoNetworkedField]
    public bool ShowAutopilotControls = true;

    [AutoNetworkedField]
    public NetEntity? FlyBy;

    [AutoNetworkedField]
    public List<Destination> Destinations = new();

    [AutoNetworkedField]
    public Dictionary<DoorLocation, bool> DoorLockStatus = new();

    [AutoNetworkedField]
    public FTLState? TravelState;

    [AutoNetworkedField]
    public StartEndTime TravelTime;

    [AutoNetworkedField]
    public string TravelDestination = string.Empty;

    [AutoNetworkedField]
    public string TravelDeparture = string.Empty;

    [AutoNetworkedField]
    public DropshipNavigationAutopilotStatus? Autopilot;
}
