using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Ladder;

[Serializable, NetSerializable]
public sealed class RadialLadderSelectedMessage(NetEntity destinationLadder) : BoundUserInterfaceMessage
{
    public NetEntity DestinationLadder = destinationLadder;
}

[Serializable, NetSerializable]
public enum LadderBuiKey : byte
{
    Key
}
