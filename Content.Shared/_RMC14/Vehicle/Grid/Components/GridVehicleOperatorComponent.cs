using Robust.Shared.GameStates;

namespace Content.Shared.Vehicle.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(VehicleSystem))]
public sealed partial class GridVehicleOperatorComponent : Component
{
}
