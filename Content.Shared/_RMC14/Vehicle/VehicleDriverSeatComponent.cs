using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedVehicleSystem))]
public sealed partial class VehicleDriverSeatComponent : Component
{
    [DataField, AutoNetworkedField]
    public SkillWhitelist Skills;

    [DataField, AutoNetworkedField]
    public EntityUid? Vehicle;
}
