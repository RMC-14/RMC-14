using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.CM14.Xenos.Acid;

public sealed partial class XenoCorrosiveAcidEvent : EntityTargetActionEvent
{
    [DataField]
    public EntProtoId AcidId = "XenoAcid";

    [DataField]
    public int PlasmaCost = 75;

    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(30);
}
