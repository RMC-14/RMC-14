using System.Collections.Generic;
using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCVehicleWeaponsSystem))]
public sealed partial class RMCVehicleWeaponsComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Operator;

    [DataField, AutoNetworkedField]
    public EntityUid? SelectedWeapon;

    [NonSerialized]
    public Dictionary<string, EntityUid> HardpointOperators = new();

    [NonSerialized]
    public Dictionary<EntityUid, string> OperatorSelections = new();
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCVehicleWeaponsSystem))]
public sealed partial class VehicleWeaponsOperatorComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Vehicle;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCVehicleWeaponsSystem))]
public sealed partial class VehicleWeaponsSeatComponent : Component
{
    [DataField, AutoNetworkedField]
    public SkillWhitelist Skills = new();
}

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCVehicleWeaponsSystem))]
public sealed partial class VehicleTurretComponent : Component
{
}
