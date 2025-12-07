using System.Collections.Generic;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCVehicleWheelSlotsComponent : Component
{
    /// <summary>
    /// Wheel slot ids that must be filled for the vehicle to operate.
    /// </summary>
    [DataField]
    public List<string> SlotIds = new()
    {
        "wheel_front_left",
        "wheel_front_right",
        "wheel_back_left",
        "wheel_back_right",
    };

    [DataField]
    public string? DefaultWheelPrototype = "RMCVanWheel";
}

/// <summary>
/// Marker component for items that can be installed as wheels.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCVehicleWheelItemComponent : Component;
