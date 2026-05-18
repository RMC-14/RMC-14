using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.ManageHive.Boons;

[Serializable, NetSerializable]
public sealed partial class HiveBoonActivateAggressionEvent : HiveBoonEvent
{
    [DataField]
    public FixedPoint2 Damage = 5;
}
