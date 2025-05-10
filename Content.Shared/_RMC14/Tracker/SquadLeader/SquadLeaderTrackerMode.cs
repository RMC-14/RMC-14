using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Tracker.SquadLeader;

[Serializable, NetSerializable]
public enum SquadLeaderTrackerMode
{
    CommandingOfficer = 0,
    ExecutiveOfficer,
    AuxiliarySupportOfficer,
    ChiefMilitaryPolice,
    ChiefEngineer,
    ChiefMedicalOfficer,
    SeniorEnlistedAdvisor,
    Quartermaster,
    MilitaryWarden,
    SquadLeader,
    FireteamLeader,
    Rifleman,
    DirectorPmc,
    LeaderPmc,
    CorporateLiaison,
    SupervisorWeYa,
    LawyerWeYa,
    LeaderGoon,
    SupervisorWeYaExecutive,
    SupervisorWeYaSpecialist,
    ProvostMarshal,
    ProvostDeputyMarshal,
    ProvostChiefInspector,
    ProvostInspector,
    ProvostTeamLeader,
    ProvostAdvisor,
    LeaderSpp,
    FreelancerLeader,
    PrimaryLandingZone,
}
