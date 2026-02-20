using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCVehicleWeaponsSystem))]
public sealed partial class RMCVehicleHardpointActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public string SlotId = string.Empty;

    [DataField, AutoNetworkedField]
    public int SortOrder;
}

public sealed partial class RMCVehicleHardpointSelectActionEvent : InstantActionEvent;
