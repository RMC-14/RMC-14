using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Ladder;

[Serializable, NetSerializable]
public enum LadderRadialBuiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class LadderRadialBuiState(NetEntity? above, NetEntity? below) : BoundUserInterfaceState
{
    public readonly NetEntity? Above = above;
    public readonly NetEntity? Below = below;
}

[Serializable, NetSerializable]
public sealed class LadderRadialSelectedMessage(NetEntity destinationLadder) : BoundUserInterfaceMessage
{
    public NetEntity DestinationLadder = destinationLadder;
}
