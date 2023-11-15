using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Xenos.Acid;

public sealed partial class XenoCorrosiveAcidEvent : EntityTargetActionEvent
{
    [DataField]
    public EntProtoId AcidId = "XenoAcid";

    [DataField]
    public FixedPoint2 PlasmaCost = 75;

    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(30);
}
