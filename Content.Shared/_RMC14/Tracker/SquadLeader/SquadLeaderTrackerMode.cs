using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Tracker.SquadLeader;

[Serializable, NetSerializable]
public enum SquadLeaderTrackerMode
{
    SquadLeader = 0,
    FireteamLeader,
}
