using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Tracker.SquadLeader;

[Serializable, NetSerializable]
public enum SquadLeaderTrackerMode
{
    CommandingOfficer = 0,
    ExecutiveOfficer,
    ChiefMilitaryPolice,
    Warden,
    SquadLeader,
    FireteamLeader,

}
