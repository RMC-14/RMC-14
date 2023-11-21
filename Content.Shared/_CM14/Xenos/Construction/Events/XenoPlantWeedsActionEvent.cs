using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Xenos.Construction.Events;

public sealed partial class XenoPlantWeedsActionEvent : InstantActionEvent
{
    [DataField]
    public FixedPoint2 PlasmaCost = 75;

    [DataField]
    public EntProtoId Prototype = "XenoWeedsSource";
}
