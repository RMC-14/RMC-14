using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Plasma;

[Serializable, NetSerializable]
public sealed partial class XenoTransferPlasmaDoAfterEvent : DoAfterEvent
{
    [DataField]
    public FixedPoint2 Cost;

    [DataField]
    public FixedPoint2 Amount = FixedPoint2.New(50);

    [DataField]
    public FixedPoint2? TargetPercentage;

    public XenoTransferPlasmaDoAfterEvent(FixedPoint2 cost, FixedPoint2 amount, FixedPoint2? targetPercentage)
    {
        Cost = cost;
        Amount = amount;
        TargetPercentage = targetPercentage;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
