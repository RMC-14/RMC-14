using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VehicleSensorComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Range = 40f;

    [DataField, AutoNetworkedField]
    public bool RequiresDeployed;

    [DataField, AutoNetworkedField]
    public ProtoId<NpcFactionPrototype> RevealFrom = "RMCXeno";

    [DataField, AutoNetworkedField]
    public ProtoId<NpcFactionPrototype> RevealTo = "UNMC";
}
