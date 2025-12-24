using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Refill;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMSolutionRefillerComponent), typeof(SharedMedicalSupplyLinkSystem))]
public sealed partial class CMMedicalSupplyLinkComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool ConnectedPort;

    [DataField, AutoNetworkedField]
    public string MedilinkRsiPath = "_RMC14/Structures/Machines/Medical/medilink.rsi";

    [DataField, AutoNetworkedField]
    public string BaseState = "medlink_green";

    [DataField, AutoNetworkedField]
    public string BaseLayerKey = "MedLinkLayerKey";
}
