using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Ladder;

[Serializable, NetSerializable]
public enum LadderRadialBuiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class LadderRadialBuiState(NetEntity above, NetEntity below, SelectionReason reason) : BoundUserInterfaceState
{
    public readonly NetEntity Above = above;
    public readonly NetEntity Below = below;
    public readonly SelectionReason Reason = reason;
}

[Serializable, NetSerializable]
public sealed class LadderRadialSelectedMessage(NetEntity destinationLadder, SelectionReason reason) : BoundUserInterfaceMessage
{
    public readonly NetEntity DestinationLadder = destinationLadder;
    public readonly SelectionReason Reason = reason;
}

/// <summary>
/// Used so that the ladder system knows what to do with the selected ladder once it's sent back.
/// </summary>
[Serializable, NetSerializable]
public enum SelectionReason : byte
{
    Climb,
    Watch
}
