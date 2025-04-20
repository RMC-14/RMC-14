using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Construction.Events;

public sealed partial class XenoExpandWeedsActionEvent : WorldTargetActionEvent
{
    [DataField]
    public EntProtoId Expand = "XenoWeeds";

    [DataField]
    public EntProtoId Source = "XenoWeedsSource";

    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = 50;
}
