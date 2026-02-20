using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedVehicleSystem))]
public sealed partial class VehicleWatchingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Watching;
}
