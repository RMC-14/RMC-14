using Robust.Shared.Map;

namespace Content.Shared._RMC14.Xenonids.Construction.Events;

public sealed partial class XenoSecreteStructureAttemptEvent : CancellableEntityEventArgs
{
    public EntityCoordinates TargetCoords;

    public XenoSecreteStructureAttemptEvent(EntityCoordinates targetCoords)
    {
        TargetCoords = targetCoords;
    }
}
