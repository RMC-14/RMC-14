using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoConstructionMinRangeComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 MinRange = 0.25;
}
