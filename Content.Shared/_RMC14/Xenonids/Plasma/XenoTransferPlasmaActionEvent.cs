using Content.Shared.Actions;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Xenonids.Plasma;

public sealed partial class XenoTransferPlasmaActionEvent : EntityTargetActionEvent
{
    [DataField]
    public FixedPoint2 Cost = 0;

    [DataField]
    public FixedPoint2 Amount = 50;

    [DataField]
    public FixedPoint2? TargetPercentage;

    [DataField]
    public float? Range = 2.5f;
}
