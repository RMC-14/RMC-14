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
    public Dictionary<SquadLeaderTrackerMode, ProtoId<JobPrototype>> RoleTrackers = new()
    {
        {SquadLeaderTrackerMode.CommandingOfficer, "CMCommandingOfficer"},
        {SquadLeaderTrackerMode.ExecutiveOfficer, "CMExecutiveOfficer}"},
        {SquadLeaderTrackerMode.AuxiliarySupportOfficer, "CMAuxiliarySupportOfficer"},
        {SquadLeaderTrackerMode.ChiefMilitaryPolice, "CMChiefMP"},
        {SquadLeaderTrackerMode.ChiefEngineer, "CMChiefEngineer"},
        {SquadLeaderTrackerMode.ChiefMedicalOfficer, "CMChiefMedicalOfficer"},
        {SquadLeaderTrackerMode.SeniorEnlistedAdvisor, "CMSeniorEnlistedAdvisor"},
        {SquadLeaderTrackerMode.Quartermaster, "CMQuartermaster"},
        {SquadLeaderTrackerMode.MilitaryWarden, "CMMilitaryWarden"},
        {SquadLeaderTrackerMode.SquadLeader, "CMSquadLeader"},
        {SquadLeaderTrackerMode.FireteamLeader, "CMFireTeamLeader"},
        {SquadLeaderTrackerMode.Rifleman, "CMRifleman"},
        {SquadLeaderTrackerMode.DirectorPmc, "RMCPMCDirector"},
        {SquadLeaderTrackerMode.LeaderPmc, "RMCPMCLeader"},
        {SquadLeaderTrackerMode.CorporateLiaison, "CMLiaison"},
        {SquadLeaderTrackerMode.SupervisorWeYa, "RMCWeYaLawyerSupervisor"},
        {SquadLeaderTrackerMode.LawyerWeYa, "RMCWeYaLawyer"},
        {SquadLeaderTrackerMode.LeaderGoon, "RMCWeYaGoonLead"},
        {SquadLeaderTrackerMode.SupervisorWeYaExecutive, "CMCorporateExecutiveSupervisor"},
        {SquadLeaderTrackerMode.SupervisorWeYaSpecialist, "CMCorporateExecutiveSpecialist"},
        {SquadLeaderTrackerMode.ProvostMarshal, "CMProvostMarshal"},
        {SquadLeaderTrackerMode.ProvostDeputyMarshal, "CMProvostDeputyMarshal"},
        {SquadLeaderTrackerMode.ProvostChiefInspector, "CMProvostChiefInspector"},
        {SquadLeaderTrackerMode.ProvostInspector, "CMProvostInspector"},
        {SquadLeaderTrackerMode.ProvostTeamLeader, "CMProvostTeamLeader"},
        {SquadLeaderTrackerMode.ProvostAdvisor, "CMProvostAdvisor"},
        {SquadLeaderTrackerMode.LeaderSpp, "RMCSPPLeader"},
        {SquadLeaderTrackerMode.FreelancerLeader, "CMFreelancerLeader"},
    };

    /// <summary>
    /// The tracker alert that should be displayed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<SquadLeaderTrackerMode, ProtoId<AlertPrototype>> TrackerAlerts = new()
    {
        {SquadLeaderTrackerMode.CommandingOfficer, "SquadTrackerCommandingOfficer"},
        {SquadLeaderTrackerMode.ExecutiveOfficer, "SquadTrackerExecutiveOfficer"},
        {SquadLeaderTrackerMode.ChiefMilitaryPolice, "SquadTrackerChiefMilitaryPolice"},
        {SquadLeaderTrackerMode.SquadLeader, "SquadTracker"},
        {SquadLeaderTrackerMode.FireteamLeader, "SquadTracker"},
        {SquadLeaderTrackerMode.CorporateLiaison, "SquadTrackerCorporateLiaison"},
        {SquadLeaderTrackerMode.LeaderPmc, "SquadTrackerPMCTeamLeader"},
        {SquadLeaderTrackerMode.PrimaryLandingZone, "SquadTrackerLandingZone"},
    };

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan UpdateAt;

    [DataField, AutoNetworkedField]
    public TimeSpan UpdateEvery = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public FireteamData Fireteams = new();

    [DataField, AutoNetworkedField]
    public SquadLeaderTrackerMode? Mode;

    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    [DataField, AutoNetworkedField]
    public HashSet<SquadLeaderTrackerMode> TrackerModes = new();

}
