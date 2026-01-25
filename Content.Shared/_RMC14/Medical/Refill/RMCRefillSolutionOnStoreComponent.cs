using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Refill;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMRefillableSolutionSystem))]
public sealed partial class RMCRefillSolutionOnStoreComponent : Component
{
    [DataField, AutoNetworkedField]
    public string SolutionId = "tank";
}
