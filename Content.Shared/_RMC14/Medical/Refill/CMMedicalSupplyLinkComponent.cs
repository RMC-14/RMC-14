using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Refill;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMSolutionRefillerComponent), typeof(RMCMedicalSupplyLinkSystem))]
public sealed partial class CMMedicalSupplyLinkComponent : Component
{
    [DataField, AutoNetworkedField]
    public string BaseState = "medlink_green";

    [DataField, AutoNetworkedField]
    public bool PortConnected;
}
