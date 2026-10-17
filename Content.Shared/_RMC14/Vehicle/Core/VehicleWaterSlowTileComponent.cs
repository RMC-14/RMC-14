namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent]
public sealed partial class VehicleWaterSlowTileComponent : Component
{
    [DataField]
    public Dictionary<VehicleWeightClass, float> SpeedFactors = new();
}
