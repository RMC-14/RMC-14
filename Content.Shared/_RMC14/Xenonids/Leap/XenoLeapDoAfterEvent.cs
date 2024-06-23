using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Leap;

[Serializable, NetSerializable]
public sealed partial class XenoLeapDoAfterEvent : DoAfterEvent
{
    [DataField]
    public NetCoordinates Coordinates;

    public XenoLeapDoAfterEvent(NetCoordinates coordinates)
    {
        Coordinates = coordinates;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
