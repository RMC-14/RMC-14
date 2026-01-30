using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoConstructionPlasmaCostComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Plasma;

    [DataField, AutoNetworkedField]
    public bool ScalingCost;
}
