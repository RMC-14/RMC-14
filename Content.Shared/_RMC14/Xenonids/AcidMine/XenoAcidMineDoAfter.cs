using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.AcidMine;

[Serializable, NetSerializable]
public sealed partial class XenoAcidMineDoAfter : SimpleDoAfterEvent
{
    [DataField]
    public NetCoordinates Coordinates;

    public XenoAcidMineDoAfter(NetCoordinates coordinates)
    {
        Coordinates = coordinates;
    }

}
