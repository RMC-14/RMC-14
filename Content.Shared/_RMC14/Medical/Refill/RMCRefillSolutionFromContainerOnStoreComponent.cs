using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Refill;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMRefillableSolutionSystem))]
public sealed partial class RMCRefillSolutionFromContainerOnStoreComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "pressurized_reagent_canister";
}
