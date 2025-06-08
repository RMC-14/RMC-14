using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Intel;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(IntelSystem))]
public sealed partial class IntelRescueSurvivorObjectiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Value = FixedPoint2.New(0.7);
}
