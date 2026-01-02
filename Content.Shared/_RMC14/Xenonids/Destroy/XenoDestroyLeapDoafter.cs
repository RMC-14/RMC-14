using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Destroy;

[Serializable, NetSerializable]
public sealed partial class XenoDestroyLeapDoafter : SimpleDoAfterEvent
{
    [DataField]
    public NetCoordinates TargetCoords;

    public XenoDestroyLeapDoafter(NetCoordinates coordinates)
    {
        TargetCoords = coordinates;
    }
}
