using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Vehicle;
[RegisterComponent]
[Access(typeof(SharedVehicleSystem))]
public sealed partial class VehicleEnterComponent : Component
{
    /// <summary>
    /// The resource path to the interior grid for the vehicle.
    /// </summary>
    [DataField]
    public ResPath? InteriorPath;
}
