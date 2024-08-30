using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Rangefinder;

[Serializable, NetSerializable]
public sealed partial class LaserDesignatorDoAfterEvent : SimpleDoAfterEvent
{
    public readonly NetCoordinates Coordinates;

    public LaserDesignatorDoAfterEvent(NetCoordinates coordinates)
    {
        Coordinates = coordinates;
    }
}
