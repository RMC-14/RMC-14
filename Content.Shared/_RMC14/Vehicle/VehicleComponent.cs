using Content.Shared.Interaction;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedVehicleSystem))]
public sealed partial class VehicleComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? Id;

    [DataField, AutoNetworkedField]
    public EntityUid? Other;
}
