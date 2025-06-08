using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Refill;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCPressurizedSolutionComponent : Component
{
    [DataField]
    public string Solution = "default";
}
