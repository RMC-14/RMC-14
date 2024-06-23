using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Entrenching;

[Serializable, NetSerializable]
public sealed partial class SandbagBuildDoAfterEvent : SimpleDoAfterEvent
{
    [DataField(required: true)]
    public NetCoordinates Coordinates;

    [DataField(required: true)]
    public Direction Direction;

    public SandbagBuildDoAfterEvent(NetCoordinates coordinates, Direction direction)
    {
        Coordinates = coordinates;
        Direction = direction;
    }
}
