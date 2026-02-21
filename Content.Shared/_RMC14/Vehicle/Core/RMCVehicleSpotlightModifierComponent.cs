namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent]
public sealed partial class RMCVehicleSpotlightModifierComponent : Component
{
    [DataField]
    public float RadiusMultiplier = 1f;

    [DataField]
    public float RadiusAdd = 0f;

    [DataField]
    public float EnergyMultiplier = 1f;

    [DataField]
    public float EnergyAdd = 0f;

    [DataField]
    public float SoftnessMultiplier = 1f;

    [DataField]
    public float SoftnessAdd = 0f;
}
