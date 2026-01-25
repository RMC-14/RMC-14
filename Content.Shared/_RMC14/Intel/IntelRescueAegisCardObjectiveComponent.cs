using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Intel;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(IntelSystem))]
public sealed partial class IntelRescueAegisCardObjectiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Value = FixedPoint2.New(1.5);
}
