using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent]
[Access(typeof(VehicleSystem))]
public sealed partial class GridVehicleOperatorComponent : Component
{
}
