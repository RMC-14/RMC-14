using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Barricade;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ProjectileCoverComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Coverage = 85;
}
