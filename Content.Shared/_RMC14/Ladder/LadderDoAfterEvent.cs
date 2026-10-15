using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Ladder;

[Serializable, NetSerializable]
public sealed partial class LadderDoAfterEvent : DoAfterEvent
{
    public NetEntity DestinationLadder;

    public LadderDoAfterEvent(NetEntity destinationLadder)
    {
        DestinationLadder = destinationLadder;
    }

    public override DoAfterEvent Clone() => this;
}
