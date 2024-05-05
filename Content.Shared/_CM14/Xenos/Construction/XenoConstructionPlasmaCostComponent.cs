using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoConstructionPlasmaCostComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Plasma;
}
