using Content.Shared.Actions;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Xenonids.ResinSurge;

public sealed partial class XenoResinSurgeActionEvent : EntityWorldTargetActionEvent
{
    [DataField]
    public FixedPoint2 PlasmaCost = 75;
}
