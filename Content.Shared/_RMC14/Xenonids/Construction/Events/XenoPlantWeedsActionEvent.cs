using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Construction.Events;

public sealed partial class XenoPlantWeedsActionEvent : InstantActionEvent
{
    [DataField]
    public FixedPoint2 PlasmaCost = 75;

    [DataField]
    public EntProtoId Prototype = "XenoWeedsSource";

    // Slight corner cutting to avoid the pain of having to extract this from the prototype every time
    // TODO: do this properly
    [DataField]
    public bool UseOnSemiWeedable = false;

    [DataField]
    public bool LimitDistance = false;
}
