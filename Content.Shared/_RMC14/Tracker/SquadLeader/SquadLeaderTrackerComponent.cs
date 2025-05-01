using Content.Shared.Alert;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Tracker.SquadLeader;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SquadLeaderTrackerSystem))]
public sealed partial class SquadLeaderTrackerComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> Alert = "SquadTracker";

    /// <summary>
    /// The tracker alerts that should be displayed based on the targeted role.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<JobPrototype>, ProtoId<AlertPrototype>> RoleTrackerAlerts = new()
    {
        {"CMCommandingOfficer", "SquadTrackerCommandingOfficer"},
        {"CMExecutiveOfficer", "SquadTrackerExecutiveOfficer"},
        {"CMChiefMP", "SquadTrackerChiefMilitaryPolice"},
        {"CMSquadLeader", "SquadTracker"},
        {"CMFireteamLeader", "SquadTracker"},
        {"CMLiaison", "SquadTrackerCorporateLiaison"},
        {"RMCPMCLeader", "SquadTrackerPMCTeamLeader"},
    };

    /// <summary>
    /// The tracker alert that should be displayed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<SquadLeaderTrackerMode, ProtoId<AlertPrototype>> ExtraModeTrackerAlerts = new()
    {
        {SquadLeaderTrackerMode.PrimaryLandingZone, "SquadTrackerLandingZone"},
    };

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan UpdateAt;

    [DataField, AutoNetworkedField]
    public TimeSpan UpdateEvery = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public FireteamData Fireteams = new();

    [DataField, AutoNetworkedField]
    public ProtoId<JobPrototype>? Role;

    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    [DataField, AutoNetworkedField]
    public List<SquadLeaderTrackerMode> ExtraModes = new();

    [DataField, AutoNetworkedField]
    public List<ProtoId<JobPrototype>> TrackableRoles = new();
}
