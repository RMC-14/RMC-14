using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Intel;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(IntelSystem))]
public sealed partial class IntelSafeObjectiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Code = string.Empty;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Value = FixedPoint2.New(0.35);

    [DataField, AutoNetworkedField]
    public bool Completed;
}
