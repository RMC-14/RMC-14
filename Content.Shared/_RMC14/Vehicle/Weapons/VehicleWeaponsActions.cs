using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VehicleWeaponsSystem))]
public sealed partial class VehicleHardpointActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? MountedWeapon;

    [DataField, AutoNetworkedField]
    public int SortOrder;
}

public sealed partial class VehicleHardpointSelectActionEvent : InstantActionEvent;
