using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Egg;

[Serializable, NetSerializable]
public sealed partial class XenoEggPlaceDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public NetCoordinates Coordinates;

    public XenoEggPlaceDoAfterEvent(NetCoordinates coordinates)
    {
        Coordinates = coordinates;
    }
}
