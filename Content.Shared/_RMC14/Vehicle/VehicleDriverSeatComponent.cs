using Content.Shared._RMC14.Marines.Skills;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent]
[Access(typeof(SharedVehicleSystem))]
public sealed partial class VehicleDriverSeatComponent : Component
{
    [DataField]
    public SkillWhitelist Skills;

    [DataField]
    public EntityUid? Vehicle;
}
