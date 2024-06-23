using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Egg;

[Serializable, NetSerializable]
public sealed partial class XenoGrowOvipositorDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public FixedPoint2 PlasmaCost;
}
