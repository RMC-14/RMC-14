namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent]
public sealed partial class VehicleCorrosiveTileComponent : Component
{
    [DataField]
    public float WheelDamage = 10f;
}
