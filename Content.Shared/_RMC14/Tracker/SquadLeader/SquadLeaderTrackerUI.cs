using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Tracker.SquadLeader;

[Serializable, NetSerializable]
public enum SquadLeaderTrackerUI
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SquadLeaderTrackerAssignFireteamMsg(NetEntity marine, int fireteam) : BoundUserInterfaceMessage
{
    public readonly NetEntity Marine = marine;
    public readonly int Fireteam = fireteam;
}

[Serializable, NetSerializable]
public sealed class SquadLeaderTrackerUnassignFireteamMsg(NetEntity marine) : BoundUserInterfaceMessage
{
    public readonly NetEntity Marine = marine;
}

[Serializable, NetSerializable]
public sealed class SquadLeaderTrackerPromoteFireteamLeaderMsg(NetEntity marine) : BoundUserInterfaceMessage
{
    public readonly NetEntity Marine = marine;
}

[Serializable, NetSerializable]
public sealed class SquadLeaderTrackerDemoteFireteamLeaderMsg(int fireteam) : BoundUserInterfaceMessage
{
    public readonly int Fireteam = fireteam;
}

[Serializable, NetSerializable]
public sealed class SquadLeaderTrackerChangeTrackedMsg : BoundUserInterfaceMessage;
