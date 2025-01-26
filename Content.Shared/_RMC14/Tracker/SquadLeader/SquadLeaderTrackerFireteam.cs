using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Tracker.SquadLeader;

[DataDefinition]
[Serializable, NetSerializable]
public partial record SquadLeaderTrackerFireteam
{
    [DataField]
    public SquadLeaderTrackerMarine? Leader;

    [DataField]
    public Dictionary<NetEntity, SquadLeaderTrackerMarine>? Members;
}
