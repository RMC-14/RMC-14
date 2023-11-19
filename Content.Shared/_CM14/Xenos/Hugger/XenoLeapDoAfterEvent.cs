using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Xenos.Hugger;

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
