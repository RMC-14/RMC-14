using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship.Utility.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCOrbitalDeployerComponent : Component
{
    [DataField, AutoNetworkedField]
    public string DeployableContainerSlotId = "rmc_orbital_deployer_deployable_container_slot";
}
