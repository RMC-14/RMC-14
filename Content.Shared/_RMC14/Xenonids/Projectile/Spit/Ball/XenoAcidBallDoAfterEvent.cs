using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Projectile.Spit.Ball;

[Serializable, NetSerializable]
public sealed partial class XenoAcidBallDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public NetCoordinates Coordinates;

    public XenoAcidBallDoAfterEvent(NetCoordinates coordinates)
    {
        Coordinates = coordinates;
    }
}
