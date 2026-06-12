using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Dropship.Utility.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCOrbitalDeployerComponent : Component
{
    [DataField, AutoNetworkedField]
    public string DeployableContainerSlotId = "rmc_orbital_deployer_deployable_container_slot";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? LaunchSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/bamf.ogg");

    [DataField, AutoNetworkedField]
    public EntProtoId DropPodPrototype = "RMCSupplyDropPod";

    [DataField, AutoNetworkedField]
    public int DropScatter;
}
