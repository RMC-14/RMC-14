using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCVehiclePortGunSystem))]
public sealed partial class VehiclePortGunComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Operator;
}

[RegisterComponent]
[Access(typeof(RMCVehiclePortGunSystem))]
public sealed partial class VehiclePortGunControllerComponent : Component
{
    [DataField]
    public string GunSlotId = "port-gun";
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCVehiclePortGunSystem))]
public sealed partial class VehiclePortGunOperatorComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Gun;

    [DataField, AutoNetworkedField]
    public EntityUid? Vehicle;

    [DataField, AutoNetworkedField]
    public EntityUid? Controller;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCVehiclePortGunSystem))]
public sealed partial class VehiclePortGunSeatComponent : Component
{
    [DataField, AutoNetworkedField]
    public SkillWhitelist Skills = new();
}
