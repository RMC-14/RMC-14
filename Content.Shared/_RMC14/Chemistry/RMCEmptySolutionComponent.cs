using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCChemistrySystem))]
public sealed partial class RMCEmptySolutionComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Solution = "pen";
}
