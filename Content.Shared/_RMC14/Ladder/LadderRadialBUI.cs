using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Ladder;

[Serializable, NetSerializable]
public enum LadderRadialAction : byte
{
    Climb,
    Watch
}

[Serializable, NetSerializable]
public sealed class LadderRadialSelectedMessage(NetEntity destinationLadder) : BoundUserInterfaceMessage
{
    public readonly NetEntity DestinationLadder = destinationLadder;
}
