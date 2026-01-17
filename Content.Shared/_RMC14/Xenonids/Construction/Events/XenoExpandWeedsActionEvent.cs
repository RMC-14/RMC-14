using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Construction.Events;

public sealed partial class XenoExpandWeedsActionEvent : WorldTargetActionEvent
{
    [DataField]
    public EntProtoId Expand = "RMCXenoWeeds";

    [DataField]
    public EntProtoId Source = "RMCXenoWeedsSource";

    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = 50;
}
