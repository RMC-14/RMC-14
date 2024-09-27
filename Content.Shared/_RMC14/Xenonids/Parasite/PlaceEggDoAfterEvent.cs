using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Parasite;

[Serializable, NetSerializable]
public sealed partial class PlaceEggDoAfterEvent : SimpleDoAfterEvent
{
    [DataField(required: true)]
    public NetCoordinates Coordinates;

    public PlaceEggDoAfterEvent(NetCoordinates coordinates)
    {
        Coordinates = coordinates;
    }
}
