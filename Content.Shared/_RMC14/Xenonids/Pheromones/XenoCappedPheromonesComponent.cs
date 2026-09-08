using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Pheromones;

[RegisterComponent, NetworkedComponent]
public sealed partial class XenoCappedPheromonesComponent : Component
{
    [DataField]
    public Dictionary<XenoPheromones, FixedPoint2> CappedPheromones = new();
}
