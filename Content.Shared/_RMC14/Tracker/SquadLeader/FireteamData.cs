using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Tracker.SquadLeader;

[DataDefinition]
[Serializable, NetSerializable]
public partial record FireteamData
{
    [DataField]
    public SquadLeaderTrackerFireteam?[] Fireteams = new SquadLeaderTrackerFireteam?[3];

    [DataField]
    public string? SquadLeader;

    [DataField]
    public NetEntity? SquadLeaderId;

    [DataField]
    public Dictionary<NetEntity, SquadLeaderTrackerMarine> Unassigned = new();
}
