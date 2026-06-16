using Robust.Shared.GameObjects;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent]
public sealed partial class VehicleDamageMultiplierComponent : Component
{
    [DataField]
    public float Multiplier = 1f;
}
