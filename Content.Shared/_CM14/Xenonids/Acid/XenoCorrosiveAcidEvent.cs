using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Xenonids.Acid;

public sealed partial class XenoCorrosiveAcidEvent : EntityTargetActionEvent
{
    [DataField]
    public EntProtoId AcidId = "XenoAcidNormal";

    [DataField]
    public FixedPoint2 PlasmaCost = 100;

    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(150);
}
